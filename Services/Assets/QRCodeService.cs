using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace erp.Services.Assets;

public interface IQRCodeService
{
    byte[] GenerateQRCode(string text, int pixelsPerModule = 10);
    string GenerateQRCodeBase64(string text, int pixelsPerModule = 10);
}

public class QRCodeService : IQRCodeService
{
    /// <summary>
    /// Gera um QR code como array de bytes (PNG)
    /// </summary>
    public byte[] GenerateQRCode(string text, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        return qrCode.GetGraphic(pixelsPerModule);
    }
    
    /// <summary>
    /// Gera um QR code como string Base64 (para exibição em HTML)
    /// </summary>
    public string GenerateQRCodeBase64(string text, int pixelsPerModule = 10)
    {
        var qrCodeBytes = GenerateQRCode(text, pixelsPerModule);
        return Convert.ToBase64String(qrCodeBytes);
    }
}
