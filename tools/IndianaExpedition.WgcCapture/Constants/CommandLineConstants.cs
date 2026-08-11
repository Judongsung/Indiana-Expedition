namespace IndianaExpedition.WgcCapture.Constants;

internal static class CommandLineConstants
{
    public const string WindowArgument = "--window";
    public const string OutputArgument = "--output";
    public const string TimeoutArgument = "--timeout-seconds";
    public const string CaptureMode = "wgc";
    public const int DefaultTimeoutSeconds = 10;
    public const int MinimumTimeoutSeconds = 1;
    public const int MaximumTimeoutSeconds = 120;
}
