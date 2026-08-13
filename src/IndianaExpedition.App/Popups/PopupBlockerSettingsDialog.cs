using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Popups
{
    internal sealed class PopupBlockerSettingsDialog : LunaForm
    {
        private readonly SettingsService _settings;
        private readonly TextBox _addressBox;
        private readonly ListBox _allowedSites;
        private readonly XpButton _addButton;
        private readonly XpButton _removeButton;
        private readonly XpButton _removeAllButton;

        internal PopupBlockerSettingsDialog(SettingsService settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Text = Strings.PopupSettingsTitle;
            SetContentClientSize(476, 356);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            var addressLabel = new Label
            {
                Text = Strings.WebsiteAddress,
                AutoSize = true,
                Location = new Point(16, 18)
            };
            _addressBox = new TextBox { Location = new Point(16, 42), Size = new Size(340, 23) };
            _addressBox.TextChanged += (sender, args) => _addButton.Enabled = !string.IsNullOrWhiteSpace(_addressBox.Text);
            _addressBox.KeyDown += OnAddressKeyDown;
            _addButton = new XpButton
            {
                Text = Strings.AddSite,
                Location = new Point(364, 40),
                Size = new Size(94, 27),
                Enabled = false
            };
            _addButton.Click += (sender, args) => AddOrigin();

            var sitesGroup = new GroupBox
            {
                Text = Strings.AllowedPopupSites,
                Location = new Point(16, 80),
                Size = new Size(442, 218)
            };
            _allowedSites = new ListBox
            {
                Location = new Point(14, 24),
                Size = new Size(306, 176),
                IntegralHeight = false
            };
            _allowedSites.SelectedIndexChanged += (sender, args) => UpdateButtons();
            _removeButton = new XpButton
            {
                Text = Strings.Remove,
                Location = new Point(328, 24),
                Size = new Size(98, 27)
            };
            _removeButton.Click += (sender, args) => RemoveSelectedOrigin();
            _removeAllButton = new XpButton
            {
                Text = Strings.RemoveAll,
                Location = new Point(328, 58),
                Size = new Size(98, 27)
            };
            _removeAllButton.Click += (sender, args) => RemoveAllOrigins();
            sitesGroup.Controls.AddRange(new Control[] { _allowedSites, _removeButton, _removeAllButton });

            var close = new XpButton
            {
                Text = Strings.Close,
                Location = new Point(364, 314),
                Size = new Size(94, 27),
                DialogResult = DialogResult.OK
            };
            ContentPanel.Controls.AddRange(new Control[] { addressLabel, _addressBox, _addButton, sitesGroup, close });
            AcceptButton = _addButton;
            CancelButton = close;
            ReloadOrigins();
        }

        private void OnAddressKeyDown(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter && _addButton.Enabled)
            {
                args.SuppressKeyPress = true;
                AddOrigin();
            }
        }

        private void AddOrigin()
        {
            if (!PopupPolicy.TryNormalizeOrigin(_addressBox.Text, out var origin))
            {
                MessageBox.Show(Strings.InvalidPopupOrigin, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var current = _settings.Current.AllowedPopupOrigins;
            if (current.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                _addressBox.Clear();
                return;
            }
            if (current.Count >= PopupPolicyConstants.MaximumAllowedOrigins)
            {
                MessageBox.Show(
                    string.Format(CultureInfo.CurrentCulture, Strings.PopupOriginLimitFormat, PopupPolicyConstants.MaximumAllowedOrigins),
                    Branding.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _settings.Update(settings => settings.AllowedPopupOrigins.Add(origin));
            _addressBox.Clear();
            ReloadOrigins(origin);
        }

        private void RemoveSelectedOrigin()
        {
            var selected = _allowedSites.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            _settings.Update(settings => settings.AllowedPopupOrigins.RemoveAll(
                origin => string.Equals(origin, selected, StringComparison.OrdinalIgnoreCase)));
            ReloadOrigins();
        }

        private void RemoveAllOrigins()
        {
            _settings.Update(settings => settings.AllowedPopupOrigins.Clear());
            ReloadOrigins();
        }

        private void ReloadOrigins(string selectOrigin = null)
        {
            _allowedSites.BeginUpdate();
            try
            {
                _allowedSites.Items.Clear();
                foreach (var origin in _settings.Current.AllowedPopupOrigins)
                {
                    _allowedSites.Items.Add(origin);
                }
            }
            finally
            {
                _allowedSites.EndUpdate();
            }

            if (!string.IsNullOrWhiteSpace(selectOrigin))
            {
                _allowedSites.SelectedItem = selectOrigin;
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _removeButton.Enabled = _allowedSites.SelectedIndex >= 0;
            _removeAllButton.Enabled = _allowedSites.Items.Count > 0;
        }
    }
}
