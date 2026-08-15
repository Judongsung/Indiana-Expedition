using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Find;
using IndianaExpedition.WebView;

namespace IndianaExpedition.WebViewSmokeTests
{
    internal static class Program
    {
        private const int ExtendedStyleNoActivate = 0x08000000;
        private const uint PositionNoActivate = 0x0010;
        private const uint PositionNoMove = 0x0002;
        private const uint PositionNoSize = 0x0001;
        private static readonly IntPtr BottomWindow = new IntPtr(1);

        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var result = 1;
            using (var form = new SmokeForm())
            {
                form.Shown += async (sender, args) =>
                {
                    try
                    {
                        await RunSmokeAsync(form).ConfigureAwait(true);
                        Console.WriteLine("PASS: 로컬 WebView2 smoke 테스트가 통과했습니다.");
                        result = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                    finally
                    {
                        form.Close();
                    }
                };
                form.Show();
                SendBehind(form.Handle);
                Application.Run(form);
            }
            return result;
        }

        private static async Task RunSmokeAsync(Form form)
        {
            var root = Path.Combine(Path.GetTempPath(), "IndianaExpedition.WebViewSmoke", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var htmlPath = Path.Combine(root, "smoke.html");
                File.WriteAllText(
                    htmlPath,
                    "<!doctype html><meta charset='utf-8'><title>Smoke</title>" +
                    "<p>Indiana Expedition smoke smoke</p>" +
                    "<button id='popup' onclick=\"window.open('about:blank','_blank')\">popup</button>");
                var navigation = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>();
                var popup = new TaskCompletionSource<bool>();
                var bindings = new WebViewEventBindings
                {
                    NavigationCompleted = (sender, args) => navigation.TrySetResult(args),
                    NewWindowRequested = (sender, args) =>
                    {
                        args.Handled = true;
                        popup.TrySetResult(true);
                    }
                };
                var environment = CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: Path.Combine(root, "profile"),
                    options: null);
                using (var controller = new WebViewHostController(
                    (Panel)form.Controls[0],
                    environment,
                    bindings))
                {
                    var webView = await controller.CreateAsync().ConfigureAwait(true);
                    webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                    var navigationResult = await WithTimeout(navigation.Task, TimeSpan.FromSeconds(15)).ConfigureAwait(true);
                    if (!navigationResult.IsSuccess)
                    {
                        throw new InvalidOperationException("로컬 HTML 탐색이 실패했습니다: " + navigationResult.WebErrorStatus);
                    }

                    using (var find = new WebViewPageFindController(webView.CoreWebView2))
                    {
                        await find.FindAsync(new PageFindCriteria { Term = "smoke" }).ConfigureAwait(true);
                        if (find.MatchCount < 2)
                        {
                            throw new InvalidOperationException("Find API가 로컬 문서의 일치 항목을 찾지 못했습니다.");
                        }
                    }

                    await webView.CoreWebView2.ExecuteScriptAsync(
                        "document.getElementById('popup').click();").ConfigureAwait(true);
                    await WithTimeout(popup.Task, TimeSpan.FromSeconds(10)).ConfigureAwait(true);
                    controller.ReleaseCurrent();
                    if (controller.Current != null)
                    {
                        throw new InvalidOperationException("WebView2 폐기 후 host가 참조를 유지합니다.");
                    }
                }

                GetWindowThreadProcessId(GetForegroundWindow(), out var foregroundProcessId);
                if (foregroundProcessId == unchecked((uint)Process.GetCurrentProcess().Id))
                {
                    throw new InvalidOperationException("smoke 테스트 프로세스가 포그라운드 창을 획득했습니다.");
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(root, true);
                }
                catch
                {
                    // Runtime file handles may be released just after environment disposal.
                }
            }
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(true) != task)
            {
                throw new TimeoutException("WebView2 smoke 작업이 제한 시간을 초과했습니다.");
            }
            return await task.ConfigureAwait(true);
        }

        private static void SendBehind(IntPtr handle)
        {
            SetWindowPos(
                handle,
                BottomWindow,
                0,
                0,
                0,
                0,
                PositionNoActivate | PositionNoMove | PositionNoSize);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr window,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        private sealed class SmokeForm : Form
        {
            internal SmokeForm()
            {
                ShowInTaskbar = false;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                Bounds = new System.Drawing.Rectangle(-1600, -1200, 640, 480);
                Controls.Add(new Panel { Dock = DockStyle.Fill });
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    var parameters = base.CreateParams;
                    parameters.ExStyle |= ExtendedStyleNoActivate;
                    return parameters;
                }
            }
        }
    }
}
