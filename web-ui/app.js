// Autonomous MCP Web Interface
class AutonomousMCPApp {
    constructor() {
        this.socket = null;
        this.isConnected = false;
        this.commandHistory = [];
        this.theme = this.getStoredTheme() || this.getSystemTheme();
        
        this.init();
    }

    init() {
        this.setupTheme();
        this.setupEventListeners();
        this.connectToServer();
        this.updateStatus();
        this.initAccordions();
    }

    // Theme Management
    getSystemTheme() {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    getStoredTheme() {
        return localStorage.getItem('autonomous-mcp-theme');
    }

    setStoredTheme(theme) {
        localStorage.setItem('autonomous-mcp-theme', theme);
    }

    setupTheme() {
        document.documentElement.setAttribute('data-theme', this.theme);
        this.updateThemeToggle();
        
        // Listen for system theme changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            if (!this.getStoredTheme()) {
                this.theme = e.matches ? 'dark' : 'light';
                document.documentElement.setAttribute('data-theme', this.theme);
                this.updateThemeToggle();
            }
        });
    }

    updateThemeToggle() {
        const themeToggle = document.getElementById('themeToggle');
        const themeIcon = themeToggle.querySelector('.theme-icon');
        
        if (this.theme === 'dark') {
            themeIcon.textContent = '☀️';
            themeToggle.setAttribute('aria-label', 'Switch to light theme');
        } else {
            themeIcon.textContent = '🌙';
            themeToggle.setAttribute('aria-label', 'Switch to dark theme');
        }
    }

    toggleTheme() {
        this.theme = this.theme === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', this.theme);
        this.setStoredTheme(this.theme);
        this.updateThemeToggle();
        
        // Add smooth transition effect
        document.body.style.transition = 'all 0.3s ease';
        setTimeout(() => {
            document.body.style.transition = '';
        }, 300);
    }

    // Event Listeners
    setupEventListeners() {
        // Theme toggle
        document.getElementById('themeToggle').addEventListener('click', () => {
            this.toggleTheme();
        });

        // Command execution
        document.getElementById('executeBtn').addEventListener('click', () => {
            this.executeCommand();
        });

        // Clear command
        document.getElementById('clearBtn').addEventListener('click', () => {
            this.clearCommand();
        });

        // Enter key to execute
        document.getElementById('commandInput').addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                e.preventDefault();
                this.executeCommand();
            }
        });

        // Quick actions
        document.querySelectorAll('.quick-action').forEach(button => {
            button.addEventListener('click', () => {
                const command = button.dataset.command;
                document.getElementById('commandInput').value = command;
                this.executeCommand();
            });
        });

        // Auto-resize textarea
        const commandInput = document.getElementById('commandInput');
        commandInput.addEventListener('input', () => {
            commandInput.style.height = 'auto';
            commandInput.style.height = commandInput.scrollHeight + 'px';
        });
    }

    // Server Connection
    connectToServer() {
        try {
            this.socket = io();
            
            this.socket.on('connect', () => {
                this.isConnected = true;
                this.updateConnectionStatus('Connected', 'connected');
                console.log('Connected to Autonomous MCP server');
            });

            this.socket.on('disconnect', () => {
                this.isConnected = false;
                this.updateConnectionStatus('Disconnected', 'disconnected');
                console.log('Disconnected from server');
            });

            this.socket.on('commandResult', (data) => {
                this.handleCommandResult(data);
            });

            this.socket.on('systemUpdate', (data) => {
                this.updateSystemMetrics(data);
            });

        } catch (error) {
            console.error('Failed to connect to server:', error);
            this.updateConnectionStatus('Connection Failed', 'error');
        }
    }

    updateConnectionStatus(text, status) {
        const statusIndicator = document.getElementById('statusIndicator');
        const statusText = statusIndicator.querySelector('.status-text');
        const statusDot = statusIndicator.querySelector('.status-dot');
        
        statusText.textContent = text;
        statusDot.className = `status-dot ${status}`;
    }

    // Command Execution
    async executeCommand() {
        const commandInput = document.getElementById('commandInput');
        const command = commandInput.value.trim();
        
        if (!command) {
            this.showNotification('Please enter a command', 'warning');
            return;
        }

        if (!this.isConnected) {
            this.showNotification('Not connected to server', 'error');
            return;
        }

        // Show loading state
        const executeBtn = document.getElementById('executeBtn');
        const originalText = executeBtn.innerHTML;
        executeBtn.innerHTML = '<span class="loading"></span> Executing...';
        executeBtn.disabled = true;

        try {
            // Add to command history
            this.addCommandResult(command, 'executing', 'Executing command...');
            
            // Send command to server
            this.socket.emit('command', { command });
            
        } catch (error) {
            console.error('Error executing command:', error);
            this.showNotification('Error executing command', 'error');
            this.addCommandResult(command, 'error', error.message);
        } finally {
            // Reset button state
            executeBtn.innerHTML = originalText;
            executeBtn.disabled = false;
        }
    }

    clearCommand() {
        document.getElementById('commandInput').value = '';
        document.getElementById('commandInput').style.height = 'auto';
    }

    // Command Results
    handleCommandResult(data) {
        if (data.success) {
            this.addCommandResult(data.command || 'Command executed', 'success', data.result || 'Success');
            this.updateMetrics('commandsExecuted', 1);
        } else {
            this.addCommandResult(data.command || 'Command failed', 'error', data.error || 'Unknown error');
        }
    }

    addCommandResult(command, status, message) {
        const result = {
            id: Date.now(),
            command,
            status,
            message,
            timestamp: new Date()
        };

        this.commandHistory.unshift(result);
        
        // Keep only last 50 results
        if (this.commandHistory.length > 50) {
            this.commandHistory = this.commandHistory.slice(0, 50);
        }

        this.updateResultsDisplay();
    }

    updateResultsDisplay() {
        const container = document.getElementById('resultsContainer');
        container.innerHTML = '';

        this.commandHistory.slice(0, 10).forEach(result => {
            const resultElement = this.createResultElement(result);
            container.appendChild(resultElement);
        });
    }

    createResultElement(result) {
        const div = document.createElement('div');
        div.className = 'result-item';
        
        const timeAgo = this.getTimeAgo(result.timestamp);
        
        div.innerHTML = `
            <div class="result-command">${this.escapeHtml(result.command)}</div>
            <div class="result-time">${timeAgo}</div>
            <div class="result-status ${result.status}">
                ${result.status === 'success' ? '✅' : result.status === 'error' ? '❌' : '⏳'} 
                ${result.status === 'success' ? 'Success' : result.status === 'error' ? 'Error' : 'Executing'}
            </div>
        `;

        return div;
    }

    // System Metrics
    updateSystemMetrics(data) {
        if (data.activeTasks !== undefined) {
            this.updateMetrics('activeTasks', data.activeTasks);
        }
        if (data.servicesOnline !== undefined) {
            this.updateMetrics('servicesOnline', data.servicesOnline);
        }
        if (data.successRate !== undefined) {
            this.updateMetrics('successRate', data.successRate + '%');
        }
    }

    updateMetrics(metricId, value) {
        const element = document.getElementById(metricId);
        if (element) {
            // Animate value change
            const currentValue = element.textContent;
            if (currentValue !== value.toString()) {
                element.style.transform = 'scale(1.1)';
                element.style.color = 'var(--galaxy-cyan)';
                
                setTimeout(() => {
                    element.textContent = value;
                    element.style.transform = 'scale(1)';
                    element.style.color = '';
                }, 150);
            }
        }
    }

    // Status Updates
    updateStatus() {
        // Simulate periodic status updates
        setInterval(() => {
            if (this.isConnected) {
                // Update active tasks (simulate)
                const activeTasks = Math.floor(Math.random() * 3);
                this.updateMetrics('activeTasks', activeTasks);
                
                // Update success rate
                const successRate = 95 + Math.floor(Math.random() * 5);
                this.updateMetrics('successRate', successRate + '%');
            }
        }, 5000);
    }

    // Notifications
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-icon">
                    ${type === 'success' ? '✅' : type === 'error' ? '❌' : type === 'warning' ? '⚠️' : 'ℹ️'}
                </span>
                <span class="notification-message">${this.escapeHtml(message)}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
            </div>
        `;

        // Add styles
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: var(--bg-secondary);
            border: 1px solid var(--accent);
            border-radius: var(--radius-lg);
            padding: var(--spacing-md);
            box-shadow: var(--shadow-lg);
            z-index: 1000;
            max-width: 400px;
            animation: slideIn 0.3s ease;
        `;

        document.body.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.style.animation = 'slideOut 0.3s ease';
                setTimeout(() => notification.remove(), 300);
            }
        }, 5000);
    }

    // Utility Functions
    getTimeAgo(date) {
        const now = new Date();
        const diff = now - date;
        const seconds = Math.floor(diff / 1000);
        const minutes = Math.floor(seconds / 60);
        const hours = Math.floor(minutes / 60);

        if (seconds < 60) return 'Just now';
        if (minutes < 60) return `${minutes}m ago`;
        if (hours < 24) return `${hours}h ago`;
        return date.toLocaleDateString();
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.autonomousApp = new AutonomousMCPApp();
});

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
    
    .notification-content {
        display: flex;
        align-items: center;
        gap: var(--spacing-sm);
    }
    
    .notification-icon {
        font-size: 1.2rem;
    }
    
    .notification-message {
        flex: 1;
        color: var(--text-primary);
        font-weight: 500;
    }
    
    .notification-close {
        background: none;
        border: none;
        color: var(--text-muted);
        cursor: pointer;
        font-size: 1.5rem;
        padding: 0;
        width: 24px;
        height: 24px;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: var(--radius-sm);
        transition: all 0.2s ease;
    }
    
    .notification-close:hover {
        background: var(--accent);
        color: var(--text-primary);
    }
    
    .notification-success {
        border-left: 4px solid var(--success);
    }
    
    .notification-error {
        border-left: 4px solid var(--error);
    }
    
    .notification-warning {
        border-left: 4px solid var(--warning);
    }
    
    .notification-info {
        border-left: 4px solid var(--info);
    }
`;
document.head.appendChild(style);

// Global Modal Functions
function openModal() {
    const modal = document.getElementById('commandModal');
    if (modal) {
        modal.classList.add('active');
    }
}

function closeModal() {
    const modal = document.getElementById('commandModal');
    if (modal) {
        modal.classList.remove('active');
    }
}

// Initialize accordions when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    const accordions = document.querySelectorAll('.accordion');
    
    accordions.forEach(accordion => {
        const header = accordion.querySelector('.accordion-header');
        if (header) {
            header.addEventListener('click', () => {
                // Close other accordions
                accordions.forEach(otherAccordion => {
                    if (otherAccordion !== accordion) {
                        otherAccordion.classList.remove('active');
                    }
                });
                
                // Toggle current accordion
                accordion.classList.toggle('active');
            });
        }
    });

    // Modal close on overlay click
    const modal = document.getElementById('commandModal');
    if (modal) {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                closeModal();
            }
        });
    }
});
