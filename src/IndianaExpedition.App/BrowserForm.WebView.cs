using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;
using IndianaExpedition.ContextMenus;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private string _recoveryUrl;

        private async Task InitializeBrowserAsync()
        {
            _browserReady = false;
            var candidate = new WebView2
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                DefaultBackgroundColor = Color.White
            };
            _webView = candidate;
            _browserHost.Controls.Clear();
            _browserHost.Controls.Add(candidate);

            try
            {
                var environment = await _application.EnvironmentTask.ConfigureAwait(true);
                await candidate.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
                ConfigureCoreWebView(candidate.CoreWebView2);
                AttachWebViewFeatures(candidate);
                _browserReady = true;

                var target = string.IsNullOrWhiteSpace(_recoveryUrl) ? _initialUrl : _recoveryUrl;
                _recoveryUrl = null;
                NavigateTo(target, allowExplicitFileUri: true);
            }
            catch
            {
                DetachWebViewFeatures(candidate);
                candidate.Dispose();
                if (ReferenceEquals(_webView, candidate))
                {
                    _webView = null;
                }
                throw;
            }
        }

        private void ConfigureCoreWebView(CoreWebView2 core)
        {
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;

            core.NavigationStarting += OnNavigationStarting;
            core.NavigationCompleted += OnNavigationCompleted;
            core.SourceChanged += OnSourceChanged;
            core.DocumentTitleChanged += OnDocumentTitleChanged;
            core.HistoryChanged += OnWebHistoryChanged;
            core.StatusBarTextChanged += OnStatusBarTextChanged;
            core.NewWindowRequested += OnNewWindowRequested;
            core.DownloadStarting += OnDownloadStarting;
            core.ProcessFailed += OnProcessFailed;
            core.PermissionRequested += OnPermissionRequested;
            core.ContextMenuRequested += OnContextMenuRequested;
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs args)
        {
            _pageFindController?.ResetSession();
            if (TryHandleUnsupportedNavigation(args.Uri, out var externalTarget))
            {
                args.Cancel = true;
                BeginInvoke(new Action(() => OpenExternalProtocol(externalTarget)));
                return;
            }

            _isLoading = true;
            _stopButton.Enabled = true;
            _progressBar.Visible = true;
            _statusLabel.Text = Strings.Loading;
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _isLoading = false;
            _stopButton.Enabled = false;
            _progressBar.Visible = false;
            _backButton.Enabled = CoreWebView?.CanGoBack == true;
            _forwardButton.Enabled = CoreWebView?.CanGoForward == true;

            if (!args.IsSuccess)
            {
                _statusLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.PageLoadFailedFormat,
                    args.WebErrorStatus);
                return;
            }

            _statusLabel.Text = Strings.Ready;
            var source = CoreWebView?.Source;
            var title = CoreWebView?.DocumentTitle;
            if (!string.IsNullOrWhiteSpace(source))
            {
                _addressBox.Text = source;
                UpdateZoneStatus(source);
                _application.RememberActiveUrl(source);

                if (!string.Equals(source, _lastRecordedUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _services.History.RecordNavigation(source, title, DateTime.UtcNow);
                    _lastRecordedUrl = source;
                }
            }

            Text = Branding.FormatWindowTitle(title);
        }

        private void OnSourceChanged(object sender, CoreWebView2SourceChangedEventArgs args)
        {
            var source = CoreWebView?.Source;
            if (!string.IsNullOrWhiteSpace(source))
            {
                _addressBox.Text = source;
                UpdateZoneStatus(source);
            }
        }

        private void OnDocumentTitleChanged(object sender, object args)
        {
            Text = Branding.FormatWindowTitle(CoreWebView?.DocumentTitle);
        }

        private void OnWebHistoryChanged(object sender, object args)
        {
            _backButton.Enabled = CoreWebView?.CanGoBack == true;
            _forwardButton.Enabled = CoreWebView?.CanGoForward == true;
        }

        private void OnStatusBarTextChanged(object sender, object args)
        {
            if (_isLoading)
            {
                return;
            }

            var text = CoreWebView?.StatusBarText;
            _statusLabel.Text = string.IsNullOrWhiteSpace(text) ? Strings.Ready : text;
        }

        private async void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs args)
        {
            if (ShouldAllowPopup(args, out var sourceOrigin))
            {
                await _application.AttachPopupAsync(args).ConfigureAwait(true);
                return;
            }

            args.Handled = true;
            EnqueueBlockedPopup(sourceOrigin, args.Uri);
        }

        private void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs args)
        {
            _application.Downloads.StartDownload(this, args);
        }

        internal void SetDownloadStatus(string status)
        {
            _statusLabel.Text = status;
        }

        private async void OnProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs args)
        {
            if (_recovering || IsDisposed)
            {
                return;
            }

            _recovering = true;
            _statusLabel.Text = Strings.RecoveringBrowser;
            try
            {
                _recoveryUrl = CoreWebView?.Source ?? _services.Settings.Current.HomeUrl;
                var old = _webView;
                _webView = null;
                _browserReady = false;
                _initializeTask = null;
                if (old != null)
                {
                    DetachWebViewFeatures(old);
                    _browserHost.Controls.Remove(old);
                    old.Dispose();
                }

                await EnsureBrowserReadyAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ShowBrowserInitializationError(ex);
            }
            finally
            {
                _recovering = false;
            }
        }

        private void OnPermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs args)
        {
            _application.PermissionPrompts.Handle(this, args);
        }

        private void OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs args)
        {
            var target = args.ContextMenuTarget;
            var model = new PageContextMenuModel(
                target?.HasLinkUri == true ? target.LinkUri : null,
                target?.HasSelection == true ? target.SelectionText : null);
            var menu = CreatePageContextMenu(model);
            var deferral = args.GetDeferral();
            args.Handled = true;
            var session = new WebViewContextMenuSession(
                menu,
                deferral);
            try
            {
                ReplaceContextMenuSession(session);
                session.Show(_webView, args.Location);
            }
            catch
            {
                args.Handled = false;
                session.Dispose();
            }
        }

        private void ShowBrowserInitializationError(Exception exception)
        {
            _browserHost.Controls.Clear();
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White,
                Padding = new Padding(40)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var body = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.None
            };
            body.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
                Text = Strings.BrowserStartFailed
            });
            body.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Margin = new Padding(0, 12, 0, 12),
                Text = exception.Message
            });
            var retry = new XpButton { Text = Strings.Retry, AutoSize = true };
            retry.Click += async (s, e) =>
            {
                _initializeTask = null;
                try
                {
                    await EnsureBrowserReadyAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    ShowBrowserInitializationError(ex);
                }
            };
            body.Controls.Add(retry);
            panel.Controls.Add(body, 0, 1);
            _browserHost.Controls.Add(panel);
        }

        private void UpdateZoneStatus(string source)
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            {
                _zoneLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ProtectedZoneFormat,
                    Strings.InternetZone);
            }
            else
            {
                _zoneLabel.Text = Strings.InternetZone;
            }
        }

        private static bool TryHandleUnsupportedNavigation(string target, out string externalTarget)
        {
            externalTarget = null;
            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeFile ||
                uri.Scheme == NavigationConstants.AboutScheme)
            {
                return false;
            }

            externalTarget = target;
            return true;
        }
    }
}
