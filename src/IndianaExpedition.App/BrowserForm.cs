using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm : LunaForm
    {
        private readonly BrowserApplicationContext _application;
        private readonly BrowserApplicationServices _services;
        private readonly string _initialUrl;
        private readonly Icon _applicationIcon;
        private readonly bool _visualTestMode;
        private readonly string _visualTestReadyFile;

        private TableLayoutPanel _rootLayout;
        private MenuStrip _menuStrip;
        private ToolStrip _navigationToolStrip;
        private ToolStrip _linksToolStrip;
        private Panel _addressPanel;
        private Label _addressLabel;
        private ComboBox _addressBox;
        private Button _goButton;
        private SplitContainer _contentSplit;
        private Panel _explorerPanel;
        private Label _explorerTitle;
        private Button _closeExplorerButton;
        private TreeView _explorerTree;
        private Panel _browserHost;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _progressBar;
        private ToolStripStatusLabel _zoneLabel;

        private ToolStripButton _backButton;
        private ToolStripButton _forwardButton;
        private ToolStripButton _stopButton;
        private ToolStripButton _refreshButton;
        private ToolStripButton _homeButton;
        private ToolStripButton _favoritesButton;
        private ToolStripButton _historyButton;
        private ToolStripMenuItem _favoritesMenu;
        private ToolStripMenuItem _linksBarMenuItem;
        private ToolStripMenuItem _statusBarMenuItem;

        private WebView2 _webView;
        private Task _initializeTask;
        private bool _browserReady;
        private bool _recovering;
        private bool _isLoading;
        private string _lastRecordedUrl;
        private ExplorerMode _explorerMode;

        private bool _fullScreen;
        private Rectangle _savedBounds;
        private FormWindowState _savedWindowState;
        private bool _savedTopMost;
        private bool _disposed;

        internal BrowserForm(
            BrowserApplicationContext application,
            string initialUrl,
            ApplicationLaunchOptions launchOptions)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            if (launchOptions == null)
            {
                throw new ArgumentNullException(nameof(launchOptions));
            }

            _services = application.Services;
            _visualTestMode = launchOptions.IsVisualTestMode;
            _visualTestReadyFile = launchOptions.VisualTestReadyFile;
            _initialUrl = string.IsNullOrWhiteSpace(initialUrl)
                ? BrowserDefaults.HomeUrl
                : initialUrl;

            _applicationIcon = XpGlyphs.CreateApplicationIcon();
            Icon = _applicationIcon;
            Text = Branding.ProductName;
            StartPosition = FormStartPosition.CenterScreen;
            Size = _visualTestMode
                ? new Size(
                    BrowserLayoutConstants.VisualReferenceWindowWidth,
                    BrowserLayoutConstants.VisualReferenceWindowHeight)
                : new Size(
                    BrowserLayoutConstants.InitialWindowWidth,
                    BrowserLayoutConstants.InitialWindowHeight);
            MinimumSize = new Size(
                BrowserLayoutConstants.MinimumWindowWidth,
                BrowserLayoutConstants.MinimumWindowHeight);
            KeyPreview = true;

            if (_visualTestMode)
            {
                PreventActivationOnShow = true;
            }

            BuildLayout();
            ApplyPersistedViewSettings();
            RebuildFavoritesMenu();
            ApplyVisualTestState(launchOptions.VisualTestState);

            _services.Favorites.Changed += OnFavoritesChanged;
            _services.History.Changed += OnHistoryChanged;
            _services.Settings.Changed += OnSettingsChanged;

            Shown += OnShown;
            Activated += OnActivated;
            FormClosing += OnFormClosing;
        }

        internal CoreWebView2 CoreWebView => _webView?.CoreWebView2;

        internal Task EnsureBrowserReadyAsync()
        {
            if (_initializeTask == null)
            {
                _initializeTask = InitializeBrowserAsync();
            }

            return _initializeTask;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.L:
                    FocusAddressBar();
                    return true;
                case Keys.Control | Keys.N:
                    _application.OpenWindow();
                    return true;
                case Keys.Control | Keys.O:
                    ShowOpenLocationDialog();
                    return true;
                case Keys.Control | Keys.D:
                    AddCurrentFavorite();
                    return true;
                case Keys.Control | Keys.I:
                    ShowFavoritesSidebar();
                    return true;
                case Keys.Control | Keys.H:
                    ShowHistorySidebar();
                    return true;
                case Keys.Alt | Keys.Left:
                    GoBack();
                    return true;
                case Keys.Alt | Keys.Right:
                    GoForward();
                    return true;
                case Keys.Alt | Keys.Home:
                    GoHome();
                    return true;
                case Keys.F5:
                case Keys.Control | Keys.R:
                    RefreshPage();
                    return true;
                case Keys.Escape:
                    StopNavigation();
                    return true;
                case Keys.F11:
                    ToggleFullScreen();
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void OnShown(object sender, EventArgs args)
        {
            if (_visualTestMode)
            {
                SendToBack();
                PrepareVisualTestSurface();
                return;
            }

            try
            {
                await EnsureBrowserReadyAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ShowBrowserInitializationError(ex);
            }
        }

        private void ApplyVisualTestState(VisualTestState state)
        {
            if (!_visualTestMode)
            {
                return;
            }

            switch (state)
            {
                case VisualTestState.Favorites:
                    ShowFavoritesSidebar();
                    break;
                case VisualTestState.History:
                    ShowHistorySidebar();
                    break;
            }
        }

        private void OnActivated(object sender, EventArgs args)
        {
            var url = CoreWebView?.Source;
            if (!string.IsNullOrWhiteSpace(url))
            {
                _application.RememberActiveUrl(url);
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs args)
        {
            var url = CoreWebView?.Source;
            if (!string.IsNullOrWhiteSpace(url))
            {
                _application.RememberActiveUrl(url);
            }
        }

        private void OnFavoritesChanged(object sender, EventArgs args)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(RebuildFavoritesMenu));
                return;
            }

            RebuildFavoritesMenu();
            if (_explorerMode == ExplorerMode.Favorites)
            {
                PopulateFavoritesTree();
            }
        }

        private void OnHistoryChanged(object sender, EventArgs args)
        {
            if (IsDisposed || _explorerMode != ExplorerMode.History)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(PopulateHistoryTree));
                return;
            }

            PopulateHistoryTree();
        }

        private void OnSettingsChanged(object sender, EventArgs args)
        {
            if (!IsDisposed)
            {
                ApplyPersistedViewSettings();
            }
        }

        private void ApplyPersistedViewSettings()
        {
            var settings = _services.Settings.Current;
            if (_linksToolStrip != null)
            {
                SetLinksBarVisible(settings.ShowLinksBar, persist: false);
            }
            if (_statusStrip != null)
            {
                SetStatusBarVisible(settings.ShowStatusBar, persist: false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                _services.Favorites.Changed -= OnFavoritesChanged;
                _services.History.Changed -= OnHistoryChanged;
                _services.Settings.Changed -= OnSettingsChanged;
                _webView?.Dispose();
                _applicationIcon?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        private enum ExplorerMode
        {
            None,
            Favorites,
            History
        }
    }
}
