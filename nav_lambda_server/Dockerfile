FROM rust:1.76 as builder
WORKDIR /usr/src/app
COPY . .
RUN cargo install --path .

FROM debian:bookworm-slim
# Install OpenSSL if needed by dependencies
RUN apt-get update && apt-get install -y libssl-dev ca-certificates && rm -rf /var/lib/apt/lists/*
COPY --from=builder /usr/local/cargo/bin/nav_lambda_server /usr/local/bin/nav_lambda_server
CMD ["nav_lambda_server"]
