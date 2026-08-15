namespace IndianaExpedition.WgcCapture.Constants;

internal static class CommandLineConstants
{
    public const string WindowArgument = "--window";
    public const string OutputArgument = "--output";
    public const string TimeoutArgument = "--timeout-seconds";
    public const string CaptureMode = "wgc";
    public const string CompareCommand = "compare";
    public const string BaselineArgument = "--baseline";
    public const string ActualArgument = "--actual";
    public const string DifferenceArgument = "--diff";
    public const string ChannelThresholdArgument = "--channel-threshold";
    public const string MaximumChangedRatioArgument = "--max-changed-ratio";
    public const string MaximumMeanErrorArgument = "--max-mean-error";
    public const int DefaultTimeoutSeconds = 10;
    public const int MinimumTimeoutSeconds = 1;
    public const int MaximumTimeoutSeconds = 120;
}

internal static class ImageComparisonConstants
{
    internal const int DefaultChannelThreshold = 12;
    internal const double DefaultMaximumChangedRatio = 0.02d;
    internal const double DefaultMaximumMeanError = 2.0d;
    internal const double MaximumRatio = 1.0d;
    internal const double MaximumChannelError = 255.0d;
}
