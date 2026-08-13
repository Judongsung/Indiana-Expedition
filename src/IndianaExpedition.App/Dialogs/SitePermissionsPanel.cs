using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Permissions;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;
using IndianaExpedition.Constants;

namespace IndianaExpedition
{
    internal sealed class SitePermissionsPanel : UserControl
    {
        private readonly ISitePermissionController _controller;
        private readonly ListView _list;
        private readonly Label _statusLabel;
        private readonly XpButton _allowButton;
        private readonly XpButton _blockButton;
        private readonly XpButton _resetButton;
        private readonly XpButton _resetAllButton;
        private bool _busy;

        internal SitePermissionsPanel(ISitePermissionController controller)
        {
            _controller = controller;
            Dock = DockStyle.Fill;
            BackColor = XpPalette.ControlFace;

            Controls.Add(new Label
            {
                Text = Strings.SitePermissionsIntro,
                Location = new Point(12, 12),
                Size = new Size(480, 36)
            });

            _list = new ListView
            {
                Location = new Point(12, 50),
                Size = new Size(486, 246),
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false
            };
            _list.Columns.Add(Strings.PermissionWebsiteColumn, 230);
            _list.Columns.Add(Strings.PermissionKindColumn, 150);
            _list.Columns.Add(Strings.PermissionStatusColumn, 86);
            _list.SelectedIndexChanged += (sender, args) => UpdateButtonStates();

            _statusLabel = new Label
            {
                Location = new Point(12, 302),
                Size = new Size(486, 34),
                ForeColor = SystemColors.GrayText
            };

            _allowButton = CreateButton(Strings.AllowPermission, 12);
            _allowButton.Click += async (sender, args) =>
                await ChangeSelectedAsync(CoreWebView2PermissionState.Allow).ConfigureAwait(true);
            _blockButton = CreateButton(Strings.BlockPermission, 106);
            _blockButton.Click += async (sender, args) =>
                await ChangeSelectedAsync(CoreWebView2PermissionState.Deny).ConfigureAwait(true);
            _resetButton = CreateButton(Strings.ResetPermission, 200);
            _resetButton.Click += async (sender, args) =>
                await ChangeSelectedAsync(CoreWebView2PermissionState.Default).ConfigureAwait(true);
            _resetAllButton = CreateButton(Strings.ResetAllPermissions, 374, 124);
            _resetAllButton.Click += async (sender, args) => await ResetAllAsync().ConfigureAwait(true);

            Controls.AddRange(new Control[]
            {
                _list,
                _statusLabel,
                _allowButton,
                _blockButton,
                _resetButton,
                _resetAllButton
            });

            Load += async (sender, args) => await RefreshAsync().ConfigureAwait(true);
            UpdateButtonStates();
        }

        internal async Task RefreshAsync()
        {
            if (_controller == null)
            {
                _statusLabel.Text = Strings.SitePermissionsUnavailable;
                UpdateButtonStates();
                return;
            }

            SetBusy(true, Strings.LoadingSitePermissions);
            try
            {
                var settings = await _controller.GetSettingsAsync().ConfigureAwait(true);
                _list.BeginUpdate();
                try
                {
                    _list.Items.Clear();
                    foreach (var setting in settings)
                    {
                        var item = new ListViewItem(setting.Origin) { Tag = setting };
                        item.SubItems.Add(PermissionKindDisplay.GetText(setting.Kind));
                        item.SubItems.Add(PermissionStateDisplay.GetText(setting.State));
                        _list.Items.Add(item);
                    }
                }
                finally
                {
                    _list.EndUpdate();
                }

                _statusLabel.Text = settings.Count == 0 ? Strings.NoSitePermissions : string.Empty;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.SitePermissionOperationFailedFormat,
                    ex.Message);
            }
            finally
            {
                SetBusy(false, _statusLabel.Text);
            }
        }

        private XpButton CreateButton(string text, int left, int width = 86)
        {
            return new XpButton
            {
                Text = text,
                Location = new Point(left, 344),
                Size = new Size(width, 27)
            };
        }

        private SitePermissionSetting SelectedSetting =>
            _list.SelectedItems.Count == 1
                ? _list.SelectedItems[0].Tag as SitePermissionSetting
                : null;

        private async Task ChangeSelectedAsync(CoreWebView2PermissionState state)
        {
            var selected = SelectedSetting;
            if (selected == null || _controller == null)
            {
                return;
            }

            SetBusy(true, Strings.LoadingSitePermissions);
            try
            {
                await _controller.SetStateAsync(selected, state).ConfigureAwait(true);
                await RefreshAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ShowOperationError(ex);
                SetBusy(false, string.Empty);
            }
        }

        private async Task ResetAllAsync()
        {
            if (_controller == null || _list.Items.Count == 0 ||
                !LunaConfirmationDialog.Confirm(
                    FindForm(),
                    Strings.ResetAllPermissionsTitle,
                    Strings.ResetAllPermissionsPrompt,
                    Strings.ResetAllPermissions))
            {
                return;
            }

            SetBusy(true, Strings.LoadingSitePermissions);
            try
            {
                await _controller.ResetAllAsync().ConfigureAwait(true);
                await RefreshAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ShowOperationError(ex);
                SetBusy(false, string.Empty);
            }
        }

        private void SetBusy(bool busy, string status)
        {
            _busy = busy;
            UseWaitCursor = busy;
            _list.Enabled = !busy && _controller != null;
            _statusLabel.Text = status;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasSelection = !_busy && SelectedSetting != null;
            _allowButton.Enabled = hasSelection;
            _blockButton.Enabled = hasSelection;
            _resetButton.Enabled = hasSelection;
            _resetAllButton.Enabled = !_busy && _controller != null && _list.Items.Count > 0;
        }

        private static void ShowOperationError(Exception exception)
        {
            MessageBox.Show(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.SitePermissionOperationFailedFormat,
                    exception.Message),
                Branding.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
