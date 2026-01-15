// NAVΛ Dashboard - Rust Asset Server
// Streams large files in chunks to prevent Unity memory crashes

use tokio::net::TcpListener;
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use std::path::Path;
use std::fs::File;
use std::io::{self, Read, BufReader};
use serde::{Serialize, Deserialize};

const CHUNK_SIZE: usize = 2 * 1024 * 1024; // 2MB chunks
const DEFAULT_PORT: u16 = 8080;

#[derive(Serialize, Deserialize, Debug)]
struct StreamingHeader {
    file_name: String,
    file_size: u64,
    chunk_size: u32,
    is_streaming: u8, // 1 = Streaming, 0 = Standard Transfer
}

#[derive(Serialize, Deserialize, Debug)]
struct ErrorResponse {
    error: String,
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let port = std::env::var("PORT")
        .unwrap_or_else(|_| DEFAULT_PORT.to_string())
        .parse::<u16>()?;

    let listener = TcpListener::bind(format!("0.0.0.0:{}", port)).await?;
    println!("[NAVΛ Server] Listening on port {}", port);
    println!("[NAVΛ Server] Ready to stream assets to Unity Dashboard");

    loop {
        match listener.accept().await {
            Ok((stream, addr)) => {
                println!("[NAVΛ Server] New connection from: {}", addr);
                tokio::spawn(async move {
                    if let Err(e) = handle_client(stream).await {
                        eprintln!("[NAVΛ Server] Error handling client: {}", e);
                    }
                });
            }
            Err(e) => {
                eprintln!("[NAVΛ Server] Accept error: {}", e);
            }
        }
    }
}

async fn handle_client(mut stream: tokio::net::TcpStream) -> Result<(), Box<dyn std::error::Error>> {
    // 1. Read request header (small packet)
    let mut header_buf = vec![0u8; 512];
    let bytes_read = stream.read(&mut header_buf).await?;
    
    if bytes_read == 0 {
        return Ok(()); // Connection closed
    }

    // 2. Parse request (simplified - in production use HTTP)
    let request_str = String::from_utf8_lossy(&header_buf[..bytes_read]);
    
    // Simple HTTP-like parsing
    if request_str.starts_with("GET /Assets/") {
        // Extract filename
        let path_start = request_str.find("/Assets/").unwrap() + 7;
        let path_end = request_str[path_start..].find(" HTTP").unwrap_or(request_str.len() - path_start);
        let file_name = &request_str[path_start..path_start + path_end];
        
        // Handle streaming request
        handle_streaming_request(stream, file_name).await?;
    } else if request_str.starts_with("POST /Assets/") {
        // Handle file upload (small files)
        handle_file_upload(stream, &request_str).await?;
    } else {
        // Send error response
        let error = ErrorResponse {
            error: "Invalid request".to_string(),
        };
        let error_json = serde_json::to_string(&error)?;
        let response = format!("HTTP/1.1 400 Bad Request\r\nContent-Length: {}\r\n\r\n{}", error_json.len(), error_json);
        stream.write_all(response.as_bytes()).await?;
    }

    Ok(())
}

async fn handle_streaming_request(
    mut stream: tokio::net::TcpStream,
    file_name: &str,
) -> Result<(), Box<dyn std::error::Error>> {
    let file_path = format!("./Assets/{}", file_name);
    
    // Check if file exists
    if !Path::new(&file_path).exists() {
        eprintln!("[NAVΛ Server] File not found: {}", file_path);
        let error = ErrorResponse {
            error: format!("File not found: {}", file_name),
        };
        let error_json = serde_json::to_string(&error)?;
        let response = format!("HTTP/1.1 404 Not Found\r\nContent-Length: {}\r\n\r\n{}", error_json.len(), error_json);
        stream.write_all(response.as_bytes()).await?;
        return Ok(());
    }

    // Get file size
    let metadata = std::fs::metadata(&file_path)?;
    let file_size = metadata.len();

    println!("[NAVΛ Server] Streaming file: {} ({} MB)", file_name, file_size / (1024 * 1024));

    // Open file for reading
    let file = File::open(&file_path)?;
    let mut reader = BufReader::new(file);

    // Send HTTP response header
    let content_type = get_content_type(file_name);
    let response_header = format!(
        "HTTP/1.1 200 OK\r\nContent-Type: {}\r\nContent-Length: {}\r\nAccept-Ranges: bytes\r\n\r\n",
        content_type, file_size
    );
    stream.write_all(response_header.as_bytes()).await?;

    // Stream file in chunks
    let mut total_sent = 0u64;
    let mut chunk = vec![0u8; CHUNK_SIZE];

    loop {
        // Read chunk from file
        let bytes_read = reader.read(&mut chunk)?;
        if bytes_read == 0 {
            break; // EOF
        }

        // Send chunk
        stream.write_all(&chunk[..bytes_read]).await?;
        total_sent += bytes_read as u64;

        // Log progress (every 10MB)
        if total_sent % (10 * 1024 * 1024) == 0 || total_sent == file_size {
            let progress = (total_sent as f64 / file_size as f64) * 100.0;
            println!(
                "[NAVΛ Server] Streaming... {:.1}% ({:.2} MB / {:.2} MB)",
                progress,
                total_sent as f64 / (1024.0 * 1024.0),
                file_size as f64 / (1024.0 * 1024.0)
            );
        }
    }

    println!("[NAVΛ Server] Streaming complete: {} ({:.2} MB)", file_name, file_size as f64 / (1024.0 * 1024.0));
    Ok(())
}

async fn handle_file_upload(
    mut stream: tokio::net::TcpStream,
    request_str: &str,
) -> Result<(), Box<dyn std::error::Error>> {
    // Handle standard file upload (small files < 100MB)
    // In production, implement proper multipart/form-data parsing
    
    println!("[NAVΛ Server] File upload request received");
    
    // For now, just acknowledge
    let response = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n";
    stream.write_all(response.as_bytes()).await?;
    
    Ok(())
}

fn get_content_type(file_name: &str) -> &str {
    let ext = Path::new(file_name)
        .extension()
        .and_then(|e| e.to_str())
        .unwrap_or("")
        .to_lowercase();

    match ext.as_str() {
        "jpg" | "jpeg" => "image/jpeg",
        "png" => "image/png",
        "fbx" => "application/octet-stream",
        "obj" => "application/octet-stream",
        "mtl" => "text/plain",
        "txt" => "text/plain",
        "json" => "application/json",
        _ => "application/octet-stream",
    }
}
