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
                _browserReady = true;

                var target = string.IsNullOrWhiteSpace(_recoveryUrl) ? _initialUrl : _recoveryUrl;
                _recoveryUrl = null;
                NavigateTo(target, allowExplicitFileUri: true);
            }
            catch
            {
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
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = true;

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
            await _application.AttachPopupAsync(args).ConfigureAwait(true);
        }

        private void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs args)
        {
            try
            {
                var directory = _services.Settings.Current.DownloadDirectory;
                Directory.CreateDirectory(directory);
                var fileName = Path.GetFileName(args.ResultFilePath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = BrowserUiConstants.DefaultDownloadFileName;
                }

                args.ResultFilePath = CreateUniqueDownloadPath(directory, fileName);
                var operation = args.DownloadOperation;
                _statusLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadInProgressFormat,
                    Path.GetFileName(args.ResultFilePath));
                operation.StateChanged += (operationSender, eventArgs) =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    BeginInvoke(new Action(() => UpdateDownloadStatus(operation)));
                };
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDownloadStatus(CoreWebView2DownloadOperation operation)
        {
            var name = Path.GetFileName(operation.ResultFilePath);
            switch (operation.State)
            {
                case CoreWebView2DownloadState.Completed:
                    _statusLabel.Text = string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.DownloadCompletedFormat,
                        name);
                    break;
                case CoreWebView2DownloadState.Interrupted:
                    _statusLabel.Text = string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.DownloadFailedFormat,
                        name);
                    break;
                default:
                    _statusLabel.Text = string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.DownloadInProgressFormat,
                        name);
                    break;
            }
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
            var answer = MessageBox.Show(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.PermissionRequestFormat,
                    args.PermissionKind,
                    args.Uri),
                Branding.ProductName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            args.State = answer == DialogResult.Yes
                ? CoreWebView2PermissionState.Allow
                : CoreWebView2PermissionState.Deny;
            args.SavesInProfile = false;
        }

        private void OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs args)
        {
            args.Handled = true;
            var menu = new ContextMenuStrip { Renderer = new Styling.XpToolStripRenderer() };

            var back = menu.Items.Add(Strings.ContextBack, null, (s, e) => GoBack());
            back.Enabled = CoreWebView?.CanGoBack == true;
            var forward = menu.Items.Add(Strings.ContextForward, null, (s, e) => GoForward());
            forward.Enabled = CoreWebView?.CanGoForward == true;
            menu.Items.Add(Strings.ContextRefresh, null, (s, e) => RefreshPage());
            menu.Items.Add(new ToolStripSeparator());

            var target = args.ContextMenuTarget;
            if (target != null && target.HasLinkUri)
            {
                var link = target.LinkUri;
                menu.Items.Add(Strings.ContextOpenLinkNewWindow, null, (s, e) => _application.OpenWindow(link));
                menu.Items.Add(Strings.ContextCopyShortcut, null, (s, e) => Clipboard.SetText(link));
                menu.Items.Add(new ToolStripSeparator());
            }

            var selectionText = target != null && target.HasSelection ? target.SelectionText : null;
            var copy = menu.Items.Add(Strings.ContextCopy, null, (s, e) =>
            {
                if (!string.IsNullOrEmpty(selectionText))
                {
                    Clipboard.SetText(selectionText);
                }
            });
            copy.Enabled = !string.IsNullOrEmpty(selectionText);
            menu.Items.Add(Strings.ContextSelectAll, null, (s, e) =>
                _ = CoreWebView?.ExecuteScriptAsync(string.Format(
                    CultureInfo.InvariantCulture,
                    BrowserScriptConstants.ExecuteCommandTemplate,
                    BrowserScriptConstants.SelectAllCommand)));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(Strings.ContextProperties) { Enabled = false });

            menu.Closed += (s, e) => menu.Dispose();
            menu.Show(_webView, args.Location);
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

        private static string CreateUniqueDownloadPath(string directory, string fileName)
        {
            var candidate = Path.Combine(directory, fileName);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            for (var index = 1; index < BrowserUiConstants.MaximumDownloadNameAttempts; index++)
            {
                candidate = Path.Combine(
                    directory,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        BrowserUiConstants.UniqueDownloadNameFormat,
                        name,
                        index,
                        extension));
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(
                directory,
                Guid.NewGuid().ToString(BrowserUiConstants.UniqueIdentifierFormat) + extension);
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
