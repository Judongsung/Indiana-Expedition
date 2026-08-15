using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Core;

namespace IndianaExpedition.VisualTestHost
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] arguments)
        {
            if (arguments.Any(argument => string.Equals(
                argument,
                "--list-visual-states",
                StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(VisualStateRegistry.ToJson());
                return 0;
            }

            var state = ReadOption(arguments, "--visual-state") ?? "Main";
            if (!VisualStateRegistry.Contains(state))
            {
                Console.Error.WriteLine("Unknown visual state: " + state);
                return 2;
            }
            var dataDirectory = ReadOption(arguments, "--visual-test-data-directory");
            if (string.IsNullOrWhiteSpace(dataDirectory))
            {
                dataDirectory = Path.Combine(
                    Path.GetTempPath(),
                    "IndianaExpedition.VisualTestHost");
            }
            var readyFile = ReadOption(arguments, "--visual-test-ready-file");
            var launchOptions = ApplicationLaunchOptions.CreateVisualTest(
                new VisualTestScenario(state),
                NoOpExternalLauncher.Instance,
                new InMemoryClipboardService(),
                readyFile ?? Path.Combine(dataDirectory, "visual.ready"));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                var services = new BrowserApplicationServices(new AppDataPaths(dataDirectory));
                using (var context = new BrowserApplicationContext(services, launchOptions))
                {
                    Application.Run(context);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static string ReadOption(string[] arguments, string name)
        {
            for (var index = 0; index < (arguments?.Length ?? 0) - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }
            return null;
        }
    }
}
