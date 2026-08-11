using System.Text.Json;
using IndianaExpedition.WgcCapture.Constants;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace IndianaExpedition.WgcCapture;

internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] arguments)
    {
        try
        {
            var options = CaptureOptions.Parse(arguments);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
            var windowHandle = new HWND(new IntPtr(options.WindowHandle));
            if (!PInvoke.IsWindow(windowHandle))
            {
                throw new ArgumentException($"존재하지 않는 창 핸들입니다: {options.WindowHandle}");
            }

            var reportedSupported = WgcCapture.IsReportedSupported();
            var result = await WgcCapture.CaptureAsync(
                windowHandle,
                timeout.Token).ConfigureAwait(false);

            if (WgcCapture.IsBlank(result.Pixels))
            {
                throw new InvalidOperationException("WGC가 비어 있거나 검은 프레임만 반환했습니다.");
            }

            PngWriter.Write(options.OutputPath, result.Pixels, result.Width, result.Height);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                path = options.OutputPath,
                width = result.Width,
                height = result.Height,
                mode = CommandLineConstants.CaptureMode,
                reportedSupported,
                windowVisible = (bool)PInvoke.IsWindowVisible(windowHandle),
            }));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
            return 1;
        }
    }
}
