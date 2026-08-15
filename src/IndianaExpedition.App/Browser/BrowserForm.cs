using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using IndianaExpedition.BrowsingData;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;
using IndianaExpedition.Permissions;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Find;
using IndianaExpedition.Commands;
using IndianaExpedition.Popups;
using IndianaExpedition.WebView;
using IndianaExpedition.VisualTesting;

namespace IndianaExpedition.Browser
{
    internal sealed partial class BrowserForm : LunaForm, IMessageFilter, IVisualTestSurface
    {
        private readonly BrowserApplicationContext _application;
        private readonly BrowserApplicationServices _services;
        private readonly string _initialUrl;
        private readonly Icon _applicationIcon;
        private ImageList _explorerImages;
        private Font _linksLabelFont;
        private Font _informationIconFont;
        private Font _explorerTitleFont;
        private readonly bool _visualTestMode;
        private readonly ApplicationBrowsingDataCleaner _applicationBrowsingDataCleaner;
        private readonly IVisualTestScenario _visualTestScenario;
        private readonly string _visualTestReadyFile;
        private readonly RecentAddressHistory _recentAddresses = new RecentAddressHistory();
        private readonly IExternalLauncher _externalLauncher;
        private readonly IClipboardService _clipboardService;
        private readonly IUiCommandExecutor _uiCommandExecutor;
        private readonly BrowserCommandCatalog _commandCatalog;
        private readonly BrowserCommandRouter _commandRouter;
        private readonly ExplorerSidebarController _sidebarController = new ExplorerSidebarController();
        private FavoritesSidebarPresenter _favoritesSidebarPresenter;
        private HistorySidebarPresenter _historySidebarPresenter;
        private readonly Dictionary<BrowserCommandId, List<ToolStripItem>> _commandItems =
            new Dictionary<BrowserCommandId, List<ToolStripItem>>();

        private TableLayoutPanel _rootLayout;
        private MenuStrip _menuStrip;
        private ToolStrip _navigationToolStrip;
        private ToolStrip _linksToolStrip;
        private Panel _informationBar;
        private Label _informationBarLabel;
        private Button _openBlockedPopupButton;
        private Button _allowPopupOriginButton;
        private Button _closeInformationBarButton;
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
        private ToolStripStatusLabel _zoomLabel;
        private ToolStripStatusLabel _zoneLabel;

        private ToolStripButton _backButton;
        private ToolStripButton _forwardButton;
        private ToolStripButton _stopButton;
        private ToolStripButton _refreshButton;
        private ToolStripButton _homeButton;
        private readonly Dictionary<ExplorerMode, ToolStripButton> _explorerButtons =
            new Dictionary<ExplorerMode, ToolStripButton>();
        private readonly Dictionary<ExplorerMode, ExplorerSidebarDefinition> _explorerSidebars =
            new Dictionary<ExplorerMode, ExplorerSidebarDefinition>();
        private ToolStripMenuItem _favoritesMenu;
        private ToolStripMenuItem _helpMenu;
        private ToolStripMenuItem _linksBarMenuItem;
        private ToolStripMenuItem _statusBarMenuItem;
        private ToolStripMenuItem _popupBlockerEnabledMenuItem;
        private readonly Dictionary<BrowserZoomLevel, ToolStripMenuItem> _zoomMenuItems =
            new Dictionary<BrowserZoomLevel, ToolStripMenuItem>();

        private readonly PopupBlockerPresenter _popupBlockerPresenter;

        private WebView2 _webView;
        private WebViewHostController _webViewHostController;
        private IPageFindController _pageFindController;
        private ISitePermissionController _sitePermissionController;
        private WebViewContextMenuSession _contextMenuSession;
        private ContextMenuStrip _visualTestContextMenu;
        private PageFindCriteria _lastFindCriteria = new PageFindCriteria();
        private Form _visualTestDialog;
        private IPageFindController _visualTestFindController;
        private Task _initializeTask;
        private bool _browserReady;
        private bool _recovering;
        private bool _isLoading;
        private string _lastRecordedUrl;

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
            _externalLauncher = launchOptions.ExternalLauncher;
            _clipboardService = launchOptions.ClipboardService;
            _uiCommandExecutor = new UiCommandExecutor(ShowCommandError);
            _commandCatalog = CreateBrowserCommandCatalog();
            _commandRouter = new BrowserCommandRouter(_commandCatalog, _uiCommandExecutor);
            _popupBlockerPresenter = new PopupBlockerPresenter(
                _services.Settings,
                url => _application.OpenWindow(url));
            _popupBlockerPresenter.StateChanged += OnPopupBlockerStateChanged;
            _applicationBrowsingDataCleaner = new ApplicationBrowsingDataCleaner(
                _services.History,
                _services.Downloads);
            _visualTestMode = launchOptions.IsVisualTestMode;
            _visualTestScenario = launchOptions.VisualTestScenario;
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

            InitializeExplorerSidebars();
            BuildLayout();
            _webViewHostController = new WebViewHostController(
                _browserHost,
                _application.EnvironmentTask,
                CreateWebViewEventBindings());
            _favoritesSidebarPresenter = new FavoritesSidebarPresenter(
                _explorerTree,
                () => _services.Favorites.Items);
            _historySidebarPresenter = new HistorySidebarPresenter(
                _explorerTree,
                () => _services.History.Items);
            ApplyPersistedViewSettings();
            RebuildFavoritesMenu();

            _services.Favorites.Changed += OnFavoritesChanged;
            _services.History.DetailedChanged += OnHistoryChanged;
            _services.Settings.Changed += OnSettingsChanged;

            Shown += OnShown;
            Activated += OnActivated;
            FormClosing += OnFormClosing;
            Application.AddMessageFilter(this);
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
            if (_commandRouter.TryExecuteShortcut(keyData))
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public bool PreFilterMessage(ref Message message)
        {
            if (_disposed ||
                (message.Msg != ApplicationConstants.WindowMessageKeyDown &&
                 message.Msg != ApplicationConstants.WindowMessageSystemKeyDown) ||
                !ReferenceEquals(Form.ActiveForm, this))
            {
                return false;
            }

            var keyData = (Keys)message.WParam.ToInt32() | ModifierKeys;
            return _commandRouter.TryExecuteShortcut(keyData);
        }

        private async void OnShown(object sender, EventArgs args)
        {
            if (_visualTestMode)
            {
                SendBehindWithoutActivation();
                _visualTestScenario.Prepare(this);
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
            if (!_application.Downloads.ConfirmOwnerClose(this, args.CloseReason))
            {
                args.Cancel = true;
                return;
            }

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
            if (_sidebarController.CurrentMode == ExplorerMode.Favorites)
            {
                _favoritesSidebarPresenter.Rebuild();
            }
        }

        private void OnHistoryChanged(object sender, HistoryChangedEventArgs args)
        {
            if (IsDisposed || _sidebarController.CurrentMode != ExplorerMode.History)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => _historySidebarPresenter.Apply(args)));
                return;
            }

            _historySidebarPresenter.Apply(args);
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
            ApplyZoomSetting(settings.DefaultZoomLevel);
            if (_popupBlockerEnabledMenuItem != null)
            {
                _popupBlockerEnabledMenuItem.Checked = settings.PopupBlockerEnabled;
            }
            RefreshCommandStates();
            if (!settings.PopupBlockerEnabled && _popupBlockerPresenter.PendingCount > 0)
            {
                DismissBlockedPopups();
            }
            else if (_informationBar != null)
            {
                SetInformationBarVisible(_popupBlockerPresenter.PendingCount > 0);
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
                Application.RemoveMessageFilter(this);
                _services.Favorites.Changed -= OnFavoritesChanged;
                _services.History.DetailedChanged -= OnHistoryChanged;
                _services.Settings.Changed -= OnSettingsChanged;
                _popupBlockerPresenter.StateChanged -= OnPopupBlockerStateChanged;
                _visualTestDialog?.Dispose();
                _visualTestContextMenu?.Dispose();
                ReplaceContextMenuSession(null);
                _visualTestFindController?.Dispose();
                DetachWebViewFeatures(_webView);
                _webViewHostController?.Dispose();
                _webView = null;
                if (_explorerTree != null)
                {
                    _explorerTree.ImageList = null;
                }
                _explorerImages?.Dispose();
                _linksLabelFont?.Dispose();
                _informationIconFont?.Dispose();
                _explorerTitleFont?.Dispose();
                _applicationIcon?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        private sealed class ExplorerSidebarDefinition
        {
            internal ExplorerSidebarDefinition(Func<string> getTitle, Action populate)
            {
                GetTitle = getTitle ?? throw new ArgumentNullException(nameof(getTitle));
                Populate = populate ?? throw new ArgumentNullException(nameof(populate));
            }

            internal Func<string> GetTitle { get; }

            internal Action Populate { get; }
        }
    }
}
