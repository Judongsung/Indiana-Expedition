using System.Globalization;
using System.IO;
using IndianaExpedition.WgcCapture.Constants;

namespace IndianaExpedition.WgcCapture;

internal sealed class ImageComparisonOptions
{
    private ImageComparisonOptions(
        string baselinePath,
        string actualPath,
        string differencePath,
        int channelThreshold,
        double maximumChangedRatio,
        double maximumMeanError)
    {
        BaselinePath = baselinePath;
        ActualPath = actualPath;
        DifferencePath = differencePath;
        ChannelThreshold = channelThreshold;
        MaximumChangedRatio = maximumChangedRatio;
        MaximumMeanError = maximumMeanError;
    }

    internal string BaselinePath { get; }
    internal string ActualPath { get; }
    internal string DifferencePath { get; }
    internal int ChannelThreshold { get; }
    internal double MaximumChangedRatio { get; }
    internal double MaximumMeanError { get; }

    internal static ImageComparisonOptions Parse(string[] arguments)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < arguments.Length; index += 2)
        {
            if (index + 1 >= arguments.Length)
            {
                throw new ArgumentException("비교 옵션에는 값이 필요합니다.");
            }
            values[arguments[index]] = arguments[index + 1];
        }

        var baseline = GetPath(values, CommandLineConstants.BaselineArgument);
        var actual = GetPath(values, CommandLineConstants.ActualArgument);
        var difference = GetPath(values, CommandLineConstants.DifferenceArgument);
        return new ImageComparisonOptions(
            baseline,
            actual,
            difference,
            ParseInt(
                values,
                CommandLineConstants.ChannelThresholdArgument,
                ImageComparisonConstants.DefaultChannelThreshold),
            ParseDouble(
                values,
                CommandLineConstants.MaximumChangedRatioArgument,
                ImageComparisonConstants.DefaultMaximumChangedRatio,
                ImageComparisonConstants.MaximumRatio),
            ParseDouble(
                values,
                CommandLineConstants.MaximumMeanErrorArgument,
                ImageComparisonConstants.DefaultMaximumMeanError,
                ImageComparisonConstants.MaximumChannelError));
    }

    private static string GetPath(IReadOnlyDictionary<string, string> values, string name)
    {
        if (!values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("필수 비교 옵션이 없습니다: " + name);
        }
        return Path.GetFullPath(value);
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string name, int fallback)
    {
        if (!values.TryGetValue(name, out var text))
        {
            return fallback;
        }
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0 || value > 255)
        {
            throw new ArgumentException("채널 임계값이 올바르지 않습니다: " + text);
        }
        return value;
    }

    private static double ParseDouble(
        IReadOnlyDictionary<string, string> values,
        string name,
        double fallback,
        double maximum)
    {
        if (!values.TryGetValue(name, out var text))
        {
            return fallback;
        }
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            value < 0d ||
            value > maximum)
        {
            throw new ArgumentException("비교 임계값이 올바르지 않습니다: " + text);
        }
        return value;
    }
}
