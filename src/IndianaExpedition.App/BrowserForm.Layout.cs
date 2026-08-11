using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private void BuildLayout()
        {
            SuspendLayout();

            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, BrowserLayoutConstants.MenuHeight));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, BrowserLayoutConstants.NavigationToolbarHeight));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, BrowserLayoutConstants.AddressBarHeight));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, BrowserLayoutConstants.LinksBarHeight));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, BrowserLayoutConstants.StatusBarHeight));

            _menuStrip = BuildMenuStrip();
            _navigationToolStrip = BuildNavigationToolStrip();
            _addressPanel = BuildAddressPanel();
            _linksToolStrip = BuildLinksToolStrip();
            _contentSplit = BuildContentArea();
            _statusStrip = BuildStatusStrip();

            _rootLayout.Controls.Add(_menuStrip, 0, 0);
            _rootLayout.Controls.Add(_navigationToolStrip, 0, 1);
            _rootLayout.Controls.Add(_addressPanel, 0, 2);
            _rootLayout.Controls.Add(_linksToolStrip, 0, 3);
            _rootLayout.Controls.Add(_contentSplit, 0, 4);
            _rootLayout.Controls.Add(_statusStrip, 0, 5);
            ContentPanel.Controls.Add(_rootLayout);
            MainMenuStrip = _menuStrip;

            ResumeLayout(true);
        }

        private MenuStrip BuildMenuStrip()
        {
            var menu = new MenuStrip
            {
                Dock = DockStyle.Fill,
                Font = this.Font,
                Renderer = new XpToolStripRenderer(),
                Padding = new Padding(3, 1, 0, 1)
            };

            var file = new ToolStripMenuItem(Strings.MenuFile);
            file.DropDownItems.Add(CreateMenuItem(Strings.NewWindow, (s, e) => _application.OpenWindow(), Keys.Control | Keys.N));
            file.DropDownItems.Add(CreateMenuItem(Strings.Open, (s, e) => ShowOpenLocationDialog(), Keys.Control | Keys.O));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(CreateDisabledMenuItem(Strings.SaveAs));
            file.DropDownItems.Add(CreateDisabledMenuItem(Strings.PageSetup));
            file.DropDownItems.Add(CreateDisabledMenuItem(Strings.Print));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(CreateDisabledMenuItem(Strings.ImportExport));
            file.DropDownItems.Add(CreateDisabledMenuItem(Strings.Properties));
            file.DropDownItems.Add(CreateDisabledMenuItem(Strings.WorkOffline));
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(CreateMenuItem(Strings.Close, (s, e) => Close(), Keys.Alt | Keys.F4));

            var edit = new ToolStripMenuItem(Strings.MenuEdit);
            edit.DropDownItems.Add(CreateMenuItem(Strings.Cut, (s, e) => ExecuteEditCommand(EditCommand.Cut), Keys.Control | Keys.X));
            edit.DropDownItems.Add(CreateMenuItem(Strings.Copy, (s, e) => ExecuteEditCommand(EditCommand.Copy), Keys.Control | Keys.C));
            edit.DropDownItems.Add(CreateMenuItem(Strings.Paste, (s, e) => ExecuteEditCommand(EditCommand.Paste), Keys.Control | Keys.V));
            edit.DropDownItems.Add(new ToolStripSeparator());
            edit.DropDownItems.Add(CreateMenuItem(Strings.SelectAll, (s, e) => ExecuteEditCommand(EditCommand.SelectAll), Keys.Control | Keys.A));
            edit.DropDownItems.Add(CreateDisabledMenuItem(Strings.Find, Keys.Control | Keys.F));

            var view = new ToolStripMenuItem(Strings.MenuView);
            var toolbars = new ToolStripMenuItem(Strings.Toolbars);
            toolbars.DropDownItems.Add(new ToolStripMenuItem(Strings.StandardButtons) { Checked = true, Enabled = false });
            toolbars.DropDownItems.Add(new ToolStripMenuItem(Strings.AddressBar) { Checked = true, Enabled = false });
            _linksBarMenuItem = new ToolStripMenuItem(Strings.LinksBar) { CheckOnClick = true };
            _linksBarMenuItem.Click += (s, e) => SetLinksBarVisible(_linksBarMenuItem.Checked, persist: true);
            toolbars.DropDownItems.Add(_linksBarMenuItem);
            view.DropDownItems.Add(toolbars);

            _statusBarMenuItem = new ToolStripMenuItem(Strings.StatusBar) { CheckOnClick = true };
            _statusBarMenuItem.Click += (s, e) => SetStatusBarVisible(_statusBarMenuItem.Checked, persist: true);
            view.DropDownItems.Add(_statusBarMenuItem);

            var explorer = new ToolStripMenuItem(Strings.ExplorerBar);
            explorer.DropDownItems.Add(CreateMenuItem(Strings.MenuFavorites, (s, e) => ShowFavoritesSidebar(), Keys.Control | Keys.I));
            explorer.DropDownItems.Add(CreateMenuItem(Strings.HistoryTitle, (s, e) => ShowHistorySidebar(), Keys.Control | Keys.H));
            view.DropDownItems.Add(explorer);

            var goTo = new ToolStripMenuItem(Strings.GoTo);
            goTo.DropDownItems.Add(CreateMenuItem(Strings.Back, (s, e) => GoBack(), Keys.Alt | Keys.Left));
            goTo.DropDownItems.Add(CreateMenuItem(Strings.Forward, (s, e) => GoForward(), Keys.Alt | Keys.Right));
            goTo.DropDownItems.Add(CreateMenuItem(Strings.Home, (s, e) => GoHome(), Keys.Alt | Keys.Home));
            view.DropDownItems.Add(goTo);
            view.DropDownItems.Add(CreateMenuItem(Strings.Stop, (s, e) => StopNavigation(), Keys.Escape));
            view.DropDownItems.Add(CreateMenuItem(Strings.Refresh, (s, e) => RefreshPage(), Keys.F5));
            view.DropDownItems.Add(new ToolStripSeparator());
            view.DropDownItems.Add(CreateDisabledMenuItem(Strings.TextSize));
            view.DropDownItems.Add(CreateDisabledMenuItem(Strings.Encoding));
            view.DropDownItems.Add(CreateDisabledMenuItem(Strings.Source));
            view.DropDownItems.Add(new ToolStripSeparator());
            view.DropDownItems.Add(CreateMenuItem(Strings.FullScreen, (s, e) => ToggleFullScreen(), Keys.F11));

            _favoritesMenu = new ToolStripMenuItem(Strings.MenuFavorites);
            _favoritesMenu.DropDownItems.Add(CreateMenuItem(Strings.AddFavorite, (s, e) => AddCurrentFavorite(), Keys.Control | Keys.D));
            _favoritesMenu.DropDownItems.Add(CreateMenuItem(Strings.OrganizeFavorites, (s, e) => ShowOrganizeFavoritesDialog()));
            _favoritesMenu.DropDownItems.Add(new ToolStripSeparator());

            var tools = new ToolStripMenuItem(Strings.MenuTools);
            tools.DropDownItems.Add(CreateMenuItem(Strings.DeleteHistory, (s, e) => ClearHistory()));
            tools.DropDownItems.Add(new ToolStripSeparator());
            tools.DropDownItems.Add(CreateDisabledMenuItem(Strings.PopupBlocker));
            tools.DropDownItems.Add(CreateDisabledMenuItem(Strings.ManageAddons));
            tools.DropDownItems.Add(CreateDisabledMenuItem(Strings.WindowsUpdate));
            tools.DropDownItems.Add(new ToolStripSeparator());
            tools.DropDownItems.Add(CreateMenuItem(Strings.InternetOptions, (s, e) => ShowInternetOptionsDialog()));

            var help = new ToolStripMenuItem(Strings.MenuHelp);
            help.DropDownItems.Add(CreateDisabledMenuItem(Strings.Contents));
            help.DropDownItems.Add(CreateDisabledMenuItem(Strings.OnlineSupport));
            help.DropDownItems.Add(new ToolStripSeparator());
            help.DropDownItems.Add(CreateMenuItem(Strings.About, (s, e) => ShowAboutDialog()));

            menu.Items.AddRange(new ToolStripItem[] { file, edit, view, _favoritesMenu, tools, help });
            return menu;
        }

        private ToolStrip BuildNavigationToolStrip()
        {
            var toolStrip = new ToolStrip
            {
                Dock = DockStyle.Fill,
                Font = this.Font,
                GripStyle = ToolStripGripStyle.Visible,
                ImageScalingSize = new Size(
                    BrowserLayoutConstants.NavigationImageSize,
                    BrowserLayoutConstants.NavigationImageSize),
                Renderer = new XpToolStripRenderer(),
                Padding = new Padding(3, 1, 3, 1)
            };

            _backButton = CreateToolbarButton(Strings.ToolbarBack, GlyphKind.Back, (s, e) => GoBack(), showText: true);
            _forwardButton = CreateToolbarButton(Strings.ToolbarForward, GlyphKind.Forward, (s, e) => GoForward(), showText: true);
            _stopButton = CreateToolbarButton(Strings.ToolbarStop, GlyphKind.Stop, (s, e) => StopNavigation());
            _refreshButton = CreateToolbarButton(Strings.ToolbarRefresh, GlyphKind.Refresh, (s, e) => RefreshPage());
            _homeButton = CreateToolbarButton(Strings.ToolbarHome, GlyphKind.Home, (s, e) => GoHome());
            _favoritesButton = CreateToolbarButton(Strings.ToolbarFavorites, GlyphKind.Favorites, (s, e) => ShowFavoritesSidebar(), showText: true);
            _historyButton = CreateToolbarButton(Strings.ToolbarHistory, GlyphKind.History, (s, e) => ShowHistorySidebar(), showText: true);

            _backButton.Enabled = false;
            _forwardButton.Enabled = false;
            _stopButton.Enabled = false;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                _backButton,
                _forwardButton,
                new ToolStripSeparator(),
                _stopButton,
                _refreshButton,
                _homeButton,
                new ToolStripSeparator(),
                _favoritesButton,
                _historyButton
            });
            return toolStrip;
        }

        private Panel BuildAddressPanel()
        {
            var panel = new XpBandPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(5, 1, 5, 1)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68f));

            _addressLabel = new Label
            {
                Text = Strings.AddressLabel,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            _addressBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.Flat,
                IntegralHeight = true
            };
            _addressBox.KeyDown += OnAddressKeyDown;

            _goButton = new XpButton
            {
                Dock = DockStyle.Fill,
                Variant = XpButtonVariant.AddressBand,
                Text = Strings.Go,
                Image = XpGlyphs.Create(GlyphKind.Go, 18),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(2, 0, 0, 0)
            };
            _goButton.Click += (s, e) => NavigateFromAddressBar();

            layout.Controls.Add(_addressLabel, 0, 0);
            layout.Controls.Add(_addressBox, 1, 0);
            layout.Controls.Add(_goButton, 2, 0);
            panel.Controls.Add(layout);
            return panel;
        }

        private ToolStrip BuildLinksToolStrip()
        {
            var toolStrip = new ToolStrip
            {
                Dock = DockStyle.Fill,
                Font = this.Font,
                GripStyle = ToolStripGripStyle.Visible,
                Renderer = new XpToolStripRenderer(),
                Padding = new Padding(3, 0, 3, 0)
            };

            toolStrip.Items.Add(new ToolStripLabel(Strings.Links) { Font = new Font(Font, FontStyle.Bold) });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(CreateLinkButton(Strings.LinkGoogle, BrowserDefaults.HomeUrl));
            toolStrip.Items.Add(CreateLinkButton(Strings.LinkWebView2, ApplicationConstants.WebView2BrowserProjectUrl));
            return toolStrip;
        }

        private SplitContainer BuildContentArea()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 4,
                IsSplitterFixed = false
            };

            _explorerPanel = new Panel { Dock = DockStyle.Fill, BackColor = XpPalette.ExplorerBody };
            var header = new XpExplorerHeaderPanel { Dock = DockStyle.Top, Height = 28 };
            _explorerTitle = new XpExplorerHeaderLabel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = XpPalette.ExplorerHeaderText,
                Font = new Font(Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(7, 0, 0, 0)
            };
            _closeExplorerButton = new XpButton
            {
                Dock = DockStyle.Right,
                Width = 28,
                Text = BrowserUiConstants.CloseGlyph,
                ForeColor = XpPalette.ExplorerHeaderText,
                Variant = XpButtonVariant.ExplorerHeader,
                TabStop = false
            };
            _closeExplorerButton.Click += (s, e) => HideExplorerSidebar();
            header.Controls.Add(_explorerTitle);
            header.Controls.Add(_closeExplorerButton);

            var images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            images.Images.Add(XpGlyphs.Create(GlyphKind.Folder, 16));
            images.Images.Add(XpGlyphs.Create(GlyphKind.Page, 16));
            images.Images.Add(XpGlyphs.Create(GlyphKind.History, 16));

            _explorerTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = XpPalette.ExplorerBody,
                HideSelection = false,
                ImageList = images,
                ShowLines = true,
                ShowPlusMinus = true
            };
            _explorerTree.NodeMouseDoubleClick += OnExplorerNodeDoubleClick;

            _explorerPanel.Controls.Add(_explorerTree);
            _explorerPanel.Controls.Add(header);
            split.Panel1.Controls.Add(_explorerPanel);

            _browserHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            split.Panel2.Controls.Add(_browserHost);
            split.Panel1Collapsed = true;
            return split;
        }

        private StatusStrip BuildStatusStrip()
        {
            var status = new StatusStrip
            {
                Dock = DockStyle.Fill,
                Font = this.Font,
                Renderer = new XpToolStripRenderer(),
                SizingGrip = true
            };
            _statusLabel = new ToolStripStatusLabel(Strings.Ready) { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _progressBar = new ToolStripProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = BrowserLayoutConstants.NavigationProgressAnimationSpeed,
                Width = 90,
                Visible = false
            };
            _zoneLabel = new ToolStripStatusLabel(Strings.InternetZone)
            {
                Image = XpGlyphs.Create(GlyphKind.Globe, 16),
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };
            status.Items.Add(_statusLabel);
            status.Items.Add(_progressBar);
            status.Items.Add(_zoneLabel);
            return status;
        }

        private ToolStripButton CreateToolbarButton(string text, GlyphKind glyph, EventHandler handler, bool showText = false)
        {
            var button = new ToolStripButton
            {
                Text = text,
                ToolTipText = text,
                Image = XpGlyphs.Create(glyph),
                DisplayStyle = showText ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image,
                ImageTransparentColor = Color.Magenta,
                AutoSize = true
            };
            button.Click += handler;
            return button;
        }

        private ToolStripButton CreateLinkButton(string text, string url)
        {
            var button = new ToolStripButton(text)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Tag = url,
                ToolTipText = url
            };
            button.Click += (s, e) => NavigateTo(url);
            return button;
        }

        private static ToolStripMenuItem CreateMenuItem(string text, EventHandler handler, Keys shortcut = Keys.None)
        {
            var item = new ToolStripMenuItem(text);
            if (shortcut != Keys.None)
            {
                item.ShortcutKeyDisplayString = new KeysConverter().ConvertToString(shortcut);
                item.ShowShortcutKeys = true;
            }
            item.Click += handler;
            return item;
        }

        private static ToolStripMenuItem CreateDisabledMenuItem(string text, Keys shortcut = Keys.None)
        {
            var item = new ToolStripMenuItem(text) { Enabled = false };
            if (shortcut != Keys.None)
            {
                item.ShortcutKeyDisplayString = new KeysConverter().ConvertToString(shortcut);
                item.ShowShortcutKeys = true;
            }
            return item;
        }

        private void SetLinksBarVisible(bool visible, bool persist)
        {
            _linksToolStrip.Visible = visible;
            _rootLayout.RowStyles[3].Height = visible ? BrowserLayoutConstants.LinksBarHeight : 0f;
            if (_linksBarMenuItem != null)
            {
                _linksBarMenuItem.Checked = visible;
            }

            if (persist)
            {
                _services.Settings.Update(settings => settings.ShowLinksBar = visible);
            }
        }

        private void SetStatusBarVisible(bool visible, bool persist)
        {
            _statusStrip.Visible = visible;
            _rootLayout.RowStyles[5].Height = visible ? BrowserLayoutConstants.StatusBarHeight : 0f;
            if (_statusBarMenuItem != null)
            {
                _statusBarMenuItem.Checked = visible;
            }

            if (persist)
            {
                _services.Settings.Update(settings => settings.ShowStatusBar = visible);
            }
        }

        private void ToggleFullScreen()
        {
            if (!_fullScreen)
            {
                _savedBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                _savedWindowState = WindowState;
                _savedTopMost = TopMost;
                _fullScreen = true;

                WindowState = FormWindowState.Normal;
                SetLunaChromeVisible(false);
                Bounds = Screen.FromControl(this).Bounds;
                TopMost = true;
                for (var index = 0; index < BrowserLayoutConstants.ChromeRowCount; index++)
                {
                    _rootLayout.RowStyles[index].Height = 0f;
                }
                _rootLayout.RowStyles[5].Height = 0f;
            }
            else
            {
                _fullScreen = false;
                TopMost = _savedTopMost;
                SetLunaChromeVisible(true);
                WindowState = FormWindowState.Normal;
                Bounds = _savedBounds;
                if (_savedWindowState == FormWindowState.Maximized)
                {
                    WindowState = FormWindowState.Maximized;
                }
                _rootLayout.RowStyles[0].Height = BrowserLayoutConstants.MenuHeight;
                _rootLayout.RowStyles[1].Height = BrowserLayoutConstants.NavigationToolbarHeight;
                _rootLayout.RowStyles[2].Height = BrowserLayoutConstants.AddressBarHeight;
                ApplyPersistedViewSettings();
            }
        }

        private enum EditCommand
        {
            Cut,
            Copy,
            Paste,
            SelectAll
        }
    }
}
