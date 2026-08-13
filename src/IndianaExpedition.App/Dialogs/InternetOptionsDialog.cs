using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;
using IndianaExpedition.Permissions;

namespace IndianaExpedition
{
    internal sealed class InternetOptionsDialog : LunaForm
    {
        private readonly SettingsService _settings;
        private readonly string _currentUrl;
        private readonly TextBox _homeBox;
        private readonly RadioButton _startHome;
        private readonly RadioButton _startLast;
        private readonly TextBox _downloadBox;
        private readonly CheckBox _askWhereToSaveDownloads;

        internal InternetOptionsDialog(
            SettingsService settings,
            string currentUrl,
            ISitePermissionController permissionController = null,
            bool showPrivacyTab = false,
            bool preventActivationOnShow = false)
        {
            _settings = settings;
            _currentUrl = currentUrl;
            var current = settings.Current;

            PreventActivationOnShow = preventActivationOnShow;

            Text = Strings.InternetOptionsTitle;
            SetContentClientSize(560, 490);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = preventActivationOnShow;
            StartPosition = preventActivationOnShow
                ? FormStartPosition.CenterScreen
                : FormStartPosition.CenterParent;

            var tabs = new TabControl { Location = new Point(12, 12), Size = new Size(536, 426) };
            var general = new TabPage(Strings.GeneralTab)
            {
                BackColor = XpPalette.ControlFace,
                UseVisualStyleBackColor = false
            };
            tabs.TabPages.Add(general);
            tabs.TabPages.Add(CreateUnavailableTab(Strings.SecurityTab));
            var privacy = new TabPage(Strings.PrivacyTab)
            {
                BackColor = XpPalette.ControlFace,
                UseVisualStyleBackColor = false
            };
            privacy.Controls.Add(new SitePermissionsPanel(permissionController));
            tabs.TabPages.Add(privacy);
            tabs.TabPages.Add(CreateUnavailableTab(Strings.ContentTab));
            tabs.TabPages.Add(CreateUnavailableTab(Strings.ConnectionsTab));
            tabs.TabPages.Add(CreateUnavailableTab(Strings.ProgramsTab));
            tabs.TabPages.Add(CreateUnavailableTab(Strings.AdvancedTab));
            if (showPrivacyTab)
            {
                tabs.SelectedTab = privacy;
            }

            var homeGroup = new GroupBox { Text = Strings.HomePageGroup, Location = new Point(12, 12), Size = new Size(504, 112) };
            _homeBox = new TextBox { Text = current.HomeUrl, Location = new Point(16, 24), Size = new Size(470, 23) };
            var useCurrent = new XpButton { Text = Strings.CurrentPageButton, Location = new Point(144, 66), Size = new Size(104, 27), Enabled = !string.IsNullOrWhiteSpace(currentUrl) };
            var useDefault = new XpButton { Text = Strings.DefaultButton, Location = new Point(256, 66), Size = new Size(104, 27) };
            var useBlank = new XpButton { Text = Strings.BlankPageButton, Location = new Point(368, 66), Size = new Size(104, 27) };
            useCurrent.Click += (sender, args) => _homeBox.Text = _currentUrl;
            useDefault.Click += (sender, args) => _homeBox.Text = BrowserDefaults.HomeUrl;
            useBlank.Click += (sender, args) => _homeBox.Text = BrowserDefaults.BlankPageUrl;
            homeGroup.Controls.AddRange(new Control[] { _homeBox, useCurrent, useDefault, useBlank });

            var startupGroup = new GroupBox { Text = Strings.StartupGroup, Location = new Point(12, 132), Size = new Size(504, 82) };
            _startHome = new RadioButton { Text = Strings.StartHome, Location = new Point(18, 24), AutoSize = true };
            _startLast = new RadioButton { Text = Strings.StartLast, Location = new Point(18, 50), AutoSize = true };
            _startHome.Checked = current.StartupMode == StartupMode.Home;
            _startLast.Checked = current.StartupMode == StartupMode.LastActivePage;
            startupGroup.Controls.AddRange(new Control[] { _startHome, _startLast });

            var historyGroup = new GroupBox { Text = Strings.HistoryGroup, Location = new Point(12, 222), Size = new Size(504, 78) };
            historyGroup.Controls.Add(new Label
            {
                Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.HistoryRetentionFormat,
                    HistoryPolicy.RetentionDays,
                    HistoryPolicy.MaximumEntries),
                AutoSize = true,
                Location = new Point(16, 25)
            });
            var clear = new XpButton { Text = Strings.DeleteBrowsingDataButton, Location = new Point(368, 40), Size = new Size(118, 27) };
            clear.Click += (sender, args) => DeleteBrowsingDataRequested?.Invoke(this, EventArgs.Empty);
            historyGroup.Controls.Add(clear);

            var downloadGroup = new GroupBox { Text = Strings.DownloadGroup, Location = new Point(12, 308), Size = new Size(504, 88) };
            _downloadBox = new TextBox { Text = current.DownloadDirectory, Location = new Point(16, 27), Size = new Size(374, 23), ReadOnly = true };
            var browse = new XpButton { Text = Strings.Browse, Location = new Point(398, 25), Size = new Size(88, 27) };
            browse.Click += (sender, args) => BrowseDownloadDirectory();
            _askWhereToSaveDownloads = new CheckBox
            {
                Text = Strings.AskWhereToSaveDownloads,
                Checked = current.AskWhereToSaveDownloads,
                AutoSize = true,
                Location = new Point(16, 56)
            };
            downloadGroup.Controls.AddRange(new Control[] { _downloadBox, browse, _askWhereToSaveDownloads });

            general.Controls.AddRange(new Control[] { homeGroup, startupGroup, historyGroup, downloadGroup });

            var ok = new XpButton { Text = Strings.Ok, Location = new Point(300, 450), Size = new Size(78, 27), DialogResult = DialogResult.OK };
            ok.Click += OnOkClicked;
            var cancel = new XpButton { Text = Strings.Cancel, Location = new Point(386, 450), Size = new Size(78, 27), DialogResult = DialogResult.Cancel };
            var apply = new XpButton { Text = Strings.Apply, Location = new Point(470, 450), Size = new Size(78, 27), Enabled = false };
            ContentPanel.Controls.AddRange(new Control[] { tabs, ok, cancel, apply });
            AcceptButton = ok;
            CancelButton = cancel;
        }

        internal event EventHandler DeleteBrowsingDataRequested;

        private static TabPage CreateUnavailableTab(string title)
        {
            var page = new TabPage(title)
            {
                BackColor = XpPalette.ControlFace,
                UseVisualStyleBackColor = false
            };
            page.Controls.Add(new Label
            {
                Text = Strings.UnavailableSetting,
                ForeColor = SystemColors.GrayText,
                AutoSize = true,
                Location = new Point(28, 32)
            });
            return page;
        }

        private void BrowseDownloadDirectory()
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = Strings.DownloadFolderDescription,
                SelectedPath = Directory.Exists(_downloadBox.Text) ? _downloadBox.Text : string.Empty,
                ShowNewFolderButton = true
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _downloadBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnOkClicked(object sender, EventArgs args)
        {
            var home = _homeBox.Text.Trim();
            if (!string.Equals(home, BrowserDefaults.BlankPageUrl, StringComparison.OrdinalIgnoreCase) &&
                (!Uri.TryCreate(home, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(Strings.InvalidHomePage, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.Update(settings =>
            {
                settings.HomeUrl = home;
                settings.StartupMode = _startLast.Checked ? StartupMode.LastActivePage : StartupMode.Home;
                settings.DownloadDirectory = _downloadBox.Text;
                settings.AskWhereToSaveDownloads = _askWhereToSaveDownloads.Checked;
            });
        }
    }
}
