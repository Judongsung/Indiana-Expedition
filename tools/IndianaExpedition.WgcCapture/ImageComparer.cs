using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace IndianaExpedition.WgcCapture;

internal sealed class ImageComparisonResult
{
    internal bool DimensionsMatch { get; init; }
    internal int Width { get; init; }
    internal int Height { get; init; }
    internal double ChangedPixelRatio { get; init; }
    internal double MeanAbsoluteRgbError { get; init; }
    internal bool Passed { get; init; }
}

internal static class ImageComparer
{
    internal static ImageComparisonResult Compare(ImageComparisonOptions options)
    {
        var baseline = ReadPixels(options.BaselinePath);
        var actual = ReadPixels(options.ActualPath);
        if (baseline.Width != actual.Width || baseline.Height != actual.Height)
        {
            WriteDimensionDifference(options.DifferencePath, baseline, actual);
            return new ImageComparisonResult
            {
                DimensionsMatch = false,
                Width = actual.Width,
                Height = actual.Height,
                ChangedPixelRatio = 1d,
                MeanAbsoluteRgbError = 255d,
                Passed = false
            };
        }

        var pixelCount = checked(baseline.Width * baseline.Height);
        var difference = new byte[baseline.Pixels.Length];
        long absoluteError = 0;
        var changedPixels = 0;
        for (var offset = 0; offset < baseline.Pixels.Length; offset += 4)
        {
            var blue = Math.Abs(baseline.Pixels[offset] - actual.Pixels[offset]);
            var green = Math.Abs(baseline.Pixels[offset + 1] - actual.Pixels[offset + 1]);
            var red = Math.Abs(baseline.Pixels[offset + 2] - actual.Pixels[offset + 2]);
            absoluteError += blue + green + red;
            if (Math.Max(red, Math.Max(green, blue)) > options.ChannelThreshold)
            {
                changedPixels++;
            }
            difference[offset] = (byte)Math.Min(255, blue * 4);
            difference[offset + 1] = (byte)Math.Min(255, green * 4);
            difference[offset + 2] = (byte)Math.Min(255, red * 4);
            difference[offset + 3] = 255;
        }

        var changedRatio = (double)changedPixels / pixelCount;
        var meanError = (double)absoluteError / (pixelCount * 3L);
        var passed = changedRatio <= options.MaximumChangedRatio &&
                     meanError <= options.MaximumMeanError;
        if (!passed)
        {
            PngWriter.Write(options.DifferencePath, difference, baseline.Width, baseline.Height);
        }
        else if (File.Exists(options.DifferencePath))
        {
            File.Delete(options.DifferencePath);
        }

        return new ImageComparisonResult
        {
            DimensionsMatch = true,
            Width = actual.Width,
            Height = actual.Height,
            ChangedPixelRatio = changedRatio,
            MeanAbsoluteRgbError = meanError,
            Passed = passed
        };
    }

    private static PixelBuffer ReadPixels(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0d);
        var stride = checked(converted.PixelWidth * 4);
        var pixels = new byte[checked(stride * converted.PixelHeight)];
        converted.CopyPixels(pixels, stride, 0);
        return new PixelBuffer(converted.PixelWidth, converted.PixelHeight, pixels);
    }

    private static void WriteDimensionDifference(string path, PixelBuffer baseline, PixelBuffer actual)
    {
        var width = Math.Max(baseline.Width, actual.Width);
        var height = Math.Max(baseline.Height, actual.Height);
        var pixels = new byte[checked(width * height * 4)];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset + 2] = 255;
            pixels[offset + 3] = 255;
        }
        PngWriter.Write(path, pixels, width, height);
    }

    private sealed record PixelBuffer(int Width, int Height, byte[] Pixels);
}
