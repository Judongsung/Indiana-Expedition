using System;
using IndianaExpedition.VisualTesting;
using IndianaExpedition.Commands;

namespace IndianaExpedition
{
    internal sealed class ApplicationLaunchOptions
    {
        private ApplicationLaunchOptions(
            IVisualTestScenario visualTestScenario,
            string visualTestReadyFile,
            IExternalLauncher externalLauncher,
            IClipboardService clipboardService)
        {
            VisualTestScenario = visualTestScenario;
            VisualTestReadyFile = visualTestReadyFile;
            ExternalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
            ClipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        }

        internal bool IsVisualTestMode => VisualTestScenario != null;
        internal IVisualTestScenario VisualTestScenario { get; }
        internal string VisualTestReadyFile { get; }
        internal IExternalLauncher ExternalLauncher { get; }
        internal IClipboardService ClipboardService { get; }

        internal static ApplicationLaunchOptions CreateProduction()
        {
            return new ApplicationLaunchOptions(
                null,
                null,
                new ShellExternalLauncher(),
                new WindowsClipboardService());
        }

        internal static ApplicationLaunchOptions CreateVisualTest(
            IVisualTestScenario scenario,
            IExternalLauncher externalLauncher,
            IClipboardService clipboardService,
            string readyFile = null)
        {
            return new ApplicationLaunchOptions(
                scenario ?? throw new ArgumentNullException(nameof(scenario)),
                readyFile,
                externalLauncher,
                clipboardService);
        }
    }
}
