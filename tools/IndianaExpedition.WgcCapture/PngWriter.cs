using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IndianaExpedition.WgcCapture.Constants;

namespace IndianaExpedition.WgcCapture;

internal static class PngWriter
{
    public static void Write(string outputPath, byte[] pixels, int width, int height)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var stride = checked(width * CaptureConstants.BytesPerPixel);
        var bitmap = BitmapSource.Create(
            width,
            height,
            CaptureConstants.BitmapDpi,
            CaptureConstants.BitmapDpi,
            PixelFormats.Bgra32,
            palette: null,
            pixels,
            stride);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }
}
