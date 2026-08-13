using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Browser;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Downloads;
using IndianaExpedition.Permissions;

namespace IndianaExpedition
{
    internal sealed class BrowserApplicationContext : ApplicationContext
    {
        private readonly BrowserApplicationServices _services;
        private readonly ApplicationLaunchOptions _launchOptions;
        private readonly HashSet<BrowserForm> _windows = new HashSet<BrowserForm>();
        private readonly Task<CoreWebView2Environment> _environmentTask;
        private readonly DownloadCoordinator _downloads;
        private readonly PermissionPromptCoordinator _permissionPrompts =
            new PermissionPromptCoordinator();
        private bool _disposed;

        internal BrowserApplicationContext(
            BrowserApplicationServices services,
            ApplicationLaunchOptions launchOptions)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _launchOptions = launchOptions ?? throw new ArgumentNullException(nameof(launchOptions));
            _downloads = new DownloadCoordinator(services.Settings, services.Downloads);
            _environmentTask = launchOptions.IsVisualTestMode
                ? Task.FromResult<CoreWebView2Environment>(null)
                : CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: services.Paths.WebView2Directory,
                    options: null);

            OpenInitialWindow();
        }

        internal BrowserApplicationServices Services => _services;

        internal Task<CoreWebView2Environment> EnvironmentTask => _environmentTask;

        internal DownloadCoordinator Downloads => _downloads;

        internal PermissionPromptCoordinator PermissionPrompts => _permissionPrompts;

        internal BrowserForm OpenWindow(string initialUrl = null)
        {
            var target = string.IsNullOrWhiteSpace(initialUrl)
                ? _launchOptions.IsVisualTestMode
                    ? BrowserDefaults.BlankPageUrl
                    : _services.Settings.Current.HomeUrl
                : initialUrl;

            var window = new BrowserForm(this, target, _launchOptions);
            _windows.Add(window);
            window.FormClosed += OnWindowClosed;
            window.Show();
            return window;
        }

        internal async Task AttachPopupAsync(CoreWebView2NewWindowRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            args.Handled = true;
            try
            {
                var window = OpenWindow(BrowserDefaults.BlankPageUrl);
                await window.EnsureBrowserReadyAsync().ConfigureAwait(true);
                args.NewWindow = window.CoreWebView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                deferral.Complete();
            }
        }

        internal void RememberActiveUrl(string url)
        {
            _services.Session.Remember(url);
        }

        private void OpenInitialWindow()
        {
            if (_launchOptions.IsVisualTestMode)
            {
                OpenWindow(BrowserDefaults.BlankPageUrl);
                return;
            }

            var settings = _services.Settings.Current;
            var session = _services.Session.Current;
            var target = settings.HomeUrl;

            if (settings.StartupMode == StartupMode.LastActivePage &&
                !string.IsNullOrWhiteSpace(session.LastActiveUrl))
            {
                target = session.LastActiveUrl;
            }

            OpenWindow(target);
        }

        private void OnWindowClosed(object sender, FormClosedEventArgs args)
        {
            if (sender is BrowserForm window)
            {
                window.FormClosed -= OnWindowClosed;
                _windows.Remove(window);
                window.Dispose();
            }

            if (_windows.Count == 0)
            {
                ExitThread();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var window in new List<BrowserForm>(_windows))
                {
                    window.Close();
                }
                _windows.Clear();
                _downloads.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
