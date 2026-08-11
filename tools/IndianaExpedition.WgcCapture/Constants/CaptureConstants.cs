namespace IndianaExpedition.WgcCapture.Constants;

internal static class CaptureConstants
{
    // D3D11_SDK_VERSION is defined as 7 in d3d11.h but is not projected by CsWin32.
    public const uint D3D11SdkVersion = 7;
    public const int FramePoolBufferCount = 2;
    public const int BytesPerPixel = 4;
    public const int MaximumBlankFrameCount = 5;
    public const int BlankFrameRetryDelayMilliseconds = 50;
    public const double BitmapDpi = 96;
}
