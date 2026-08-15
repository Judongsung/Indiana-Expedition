using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Constants;
using IndianaExpedition.Core;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Dialogs;
using IndianaExpedition.Resources;

namespace IndianaExpedition
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            var launchOptions = ApplicationLaunchOptions.CreateProduction();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += OnThreadException;

            try
            {
                var paths = AppDataPaths.CreateDefault(ApplicationConstants.DataDirectoryName);
                var services = new BrowserApplicationServices(paths);
                ApplyCulture(services.Settings.Current.UiCulture);
                if (!EnsureWebView2Runtime())
                {
                    return;
                }

                using (var context = new BrowserApplicationContext(services, launchOptions))
                {
                    Application.Run(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    Branding.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool EnsureWebView2Runtime()
        {
            string detectedVersion;
            RuntimeRequirementState failureState;
            try
            {
                detectedVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (CoreWebView2Environment.CompareBrowserVersions(
                        detectedVersion,
                        WebViewRuntimeConstants.MinimumVersion) >= 0)
                {
                    return true;
                }

                failureState = RuntimeRequirementState.UpdateRequired;
            }
            catch (Exception ex)
            {
                detectedVersion = null;
                failureState = RuntimeRequirementState.InstallRequired;
                Trace.TraceError(ex.ToString());
            }

            using (var dialog = new RuntimeMissingDialog(failureState, detectedVersion))
            {
                dialog.ShowDialog();
            }
            return false;
        }

        private static void ApplyCulture(string cultureName)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureName ?? BrowserDefaults.UiCultureName);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                Strings.Culture = culture;
            }
            catch (CultureNotFoundException)
            {
                var fallback = CultureInfo.GetCultureInfo(BrowserDefaults.UiCultureName);
                Thread.CurrentThread.CurrentCulture = fallback;
                Thread.CurrentThread.CurrentUICulture = fallback;
                Strings.Culture = fallback;
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs args)
        {
            MessageBox.Show(
                args.Exception.Message,
                Branding.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
