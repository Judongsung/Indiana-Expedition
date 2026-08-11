using System.Globalization;
using System.IO;
using IndianaExpedition.WgcCapture.Constants;

namespace IndianaExpedition.WgcCapture;

internal sealed class CaptureOptions
{
    private CaptureOptions(long windowHandle, string outputPath, int timeoutSeconds)
    {
        WindowHandle = windowHandle;
        OutputPath = outputPath;
        TimeoutSeconds = timeoutSeconds;
    }

    public long WindowHandle { get; }

    public string OutputPath { get; }

    public int TimeoutSeconds { get; }

    public static CaptureOptions Parse(string[] arguments)
    {
        var values = ParsePairs(arguments);
        var windowText = GetRequiredValue(values, CommandLineConstants.WindowArgument);
        var outputPath = GetRequiredValue(values, CommandLineConstants.OutputArgument);
        var timeoutSeconds = ParseTimeout(values);

        if (!TryParseWindowHandle(windowText, out var windowHandle) || windowHandle == 0)
        {
            throw new ArgumentException($"올바르지 않은 창 핸들입니다: {windowText}");
        }

        return new CaptureOptions(
            windowHandle,
            Path.GetFullPath(outputPath),
            timeoutSeconds);
    }

    private static Dictionary<string, string> ParsePairs(string[] arguments)
    {
        if (arguments.Length % 2 != 0)
        {
            throw new ArgumentException("모든 옵션에는 값이 필요합니다.");
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < arguments.Length; index += 2)
        {
            values[arguments[index]] = arguments[index + 1];
        }

        return values;
    }

    private static string GetRequiredValue(
        IReadOnlyDictionary<string, string> values,
        string argumentName)
    {
        if (!values.TryGetValue(argumentName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"필수 옵션이 없습니다: {argumentName}");
        }

        return value;
    }

    private static int ParseTimeout(IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue(CommandLineConstants.TimeoutArgument, out var timeoutText))
        {
            return CommandLineConstants.DefaultTimeoutSeconds;
        }

        if (!int.TryParse(timeoutText, NumberStyles.None, CultureInfo.InvariantCulture, out var timeoutSeconds) ||
            timeoutSeconds < CommandLineConstants.MinimumTimeoutSeconds ||
            timeoutSeconds > CommandLineConstants.MaximumTimeoutSeconds)
        {
            throw new ArgumentException(
                $"캡처 제한 시간은 {CommandLineConstants.MinimumTimeoutSeconds}~" +
                $"{CommandLineConstants.MaximumTimeoutSeconds}초여야 합니다.");
        }

        return timeoutSeconds;
    }

    private static bool TryParseWindowHandle(string value, out long windowHandle)
    {
        const string HexadecimalPrefix = "0x";
        if (value.StartsWith(HexadecimalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(
                value[HexadecimalPrefix.Length..],
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out windowHandle);
        }

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out windowHandle);
    }
}
