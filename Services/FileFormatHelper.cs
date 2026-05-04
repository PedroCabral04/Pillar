using MudBlazor;

namespace erp.Services;

public static class FileFormatHelper
{
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static string GetAttachmentIcon(string contentType)
    {
        if (contentType.StartsWith("image/"))
            return Icons.Material.Filled.Image;
        if (contentType.Contains("pdf"))
            return Icons.Material.Filled.PictureAsPdf;
        return Icons.Material.Filled.AttachFile;
    }
}
