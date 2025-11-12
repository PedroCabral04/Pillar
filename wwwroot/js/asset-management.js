// Asset Management UI Helper Functions

// Download file from URL
window.downloadFile = function(url, fileName) {
    fetch(url)
        .then(response => response.blob())
        .then(blob => {
            const link = document.createElement('a');
            link.href = URL.createObjectURL(blob);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(link.href);
        })
        .catch(error => {
            console.error('Error downloading file:', error);
            alert('Erro ao baixar arquivo');
        });
};

// Print HTML content
window.printHtml = function(htmlContent) {
    const printWindow = window.open('', '_blank', 'width=800,height=600');
    if (printWindow) {
        printWindow.document.write(htmlContent);
        printWindow.document.close();
        printWindow.onload = function() {
            printWindow.print();
            // Optional: close window after printing
            // printWindow.close();
        };
    } else {
        alert('Pop-up bloqueado. Por favor, habilite pop-ups para este site.');
    }
};

// Get base URL of the application
window.getBaseUrl = function() {
    return window.location.origin;
};

// Copy text to clipboard (alternative method for older browsers)
window.copyToClipboard = function(text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
        return navigator.clipboard.writeText(text);
    } else {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        
        try {
            document.execCommand('copy');
            document.body.removeChild(textArea);
            return Promise.resolve();
        } catch (err) {
            document.body.removeChild(textArea);
            return Promise.reject(err);
        }
    }
};

// Preview image before upload
window.previewImage = function(inputElement, imgElementId) {
    const file = inputElement.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const img = document.getElementById(imgElementId);
            if (img) {
                img.src = e.target.result;
            }
        };
        reader.readAsDataURL(file);
    }
};

// Format file size for display
window.formatFileSize = function(bytes) {
    const sizes = ['B', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 B';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
};

// Scan QR code using device camera (for mobile)
window.scanQRCode = async function(videoElementId, canvasElementId) {
    try {
        const video = document.getElementById(videoElementId);
        const canvas = document.getElementById(canvasElementId);
        
        if (!video || !canvas) {
            throw new Error('Video or canvas element not found');
        }

        // Request camera access
        const stream = await navigator.mediaDevices.getUserMedia({ 
            video: { facingMode: 'environment' } // Use back camera on mobile
        });
        
        video.srcObject = stream;
        video.play();

        return {
            success: true,
            message: 'Camera iniciada'
        };
    } catch (error) {
        console.error('Error accessing camera:', error);
        return {
            success: false,
            message: 'Erro ao acessar câmera: ' + error.message
        };
    }
};

// Stop camera stream
window.stopCamera = function(videoElementId) {
    const video = document.getElementById(videoElementId);
    if (video && video.srcObject) {
        const stream = video.srcObject;
        const tracks = stream.getTracks();
        tracks.forEach(track => track.stop());
        video.srcObject = null;
    }
};

// Generate printable asset label
window.printAssetLabel = function(assetCode, assetName, qrCodeBase64) {
    const labelHtml = `
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <title>Etiqueta - ${assetCode}</title>
            <style>
                @page {
                    size: 4in 2in;
                    margin: 0;
                }
                body {
                    margin: 0;
                    padding: 0;
                    font-family: Arial, sans-serif;
                }
                .label {
                    width: 4in;
                    height: 2in;
                    padding: 0.2in;
                    box-sizing: border-box;
                    display: flex;
                    flex-direction: row;
                    align-items: center;
                    border: 1px solid #000;
                }
                .qr-section {
                    flex-shrink: 0;
                    margin-right: 0.2in;
                }
                .qr-section img {
                    width: 1.5in;
                    height: 1.5in;
                }
                .info-section {
                    flex-grow: 1;
                    display: flex;
                    flex-direction: column;
                    justify-content: center;
                }
                .asset-name {
                    font-size: 14pt;
                    font-weight: bold;
                    margin-bottom: 0.1in;
                    word-wrap: break-word;
                }
                .asset-code {
                    font-family: 'Courier New', monospace;
                    font-size: 12pt;
                    font-weight: bold;
                }
            </style>
        </head>
        <body>
            <div class="label">
                <div class="qr-section">
                    <img src="data:image/png;base64,${qrCodeBase64}" alt="QR Code"/>
                </div>
                <div class="info-section">
                    <div class="asset-name">${assetName}</div>
                    <div class="asset-code">${assetCode}</div>
                </div>
            </div>
        </body>
        </html>
    `;

    const printWindow = window.open('', '_blank', 'width=600,height=400');
    if (printWindow) {
        printWindow.document.write(labelHtml);
        printWindow.document.close();
        printWindow.onload = function() {
            printWindow.print();
        };
    } else {
        alert('Pop-up bloqueado. Por favor, habilite pop-ups para este site.');
    }
};

// Initialize asset management UI
window.assetManagementUI = {
    initialized: false,
    
    init: function() {
        if (this.initialized) return;
        
        console.log('Asset Management UI initialized');
        this.initialized = true;
    }
};

// Auto-initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        window.assetManagementUI.init();
    });
} else {
    window.assetManagementUI.init();
}
