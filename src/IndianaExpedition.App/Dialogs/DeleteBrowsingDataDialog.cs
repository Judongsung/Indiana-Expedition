using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed class DeleteBrowsingDataDialog : LunaForm
    {
        private readonly Func<BrowsingDataSelection, Task> _deleteAction;
        private readonly Dictionary<BrowsingDataSelection, CheckBox> _items =
            new Dictionary<BrowsingDataSelection, CheckBox>();
        private readonly Dictionary<BrowsingDataSelection, bool> _availability =
            new Dictionary<BrowsingDataSelection, bool>();
        private readonly XpButton _deleteButton;
        private readonly XpButton _cancelButton;
        private readonly Label _messageLabel;
        private readonly string _idleMessage;
        private readonly Color _idleMessageColor;
        private bool _busy;

        internal DeleteBrowsingDataDialog(
            Func<BrowsingDataSelection, Task> deleteAction,
            bool profileAvailable,
            bool preventActivationOnShow = false)
        {
            _deleteAction = deleteAction ?? throw new ArgumentNullException(nameof(deleteAction));
            PreventActivationOnShow = preventActivationOnShow;
            Text = Strings.DeleteBrowsingDataTitle;
            SetContentClientSize(526, 458);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = preventActivationOnShow;
            StartPosition = FormStartPosition.CenterParent;

            var intro = new Label
            {
                Text = Strings.DeleteBrowsingDataIntro,
                Location = new Point(18, 18),
                Size = new Size(490, 42)
            };
            var group = new GroupBox
            {
                Text = Strings.DeleteBrowsingDataTitle,
                Location = new Point(18, 68),
                Size = new Size(490, 252)
            };

            AddOption(group, BrowsingDataSelection.History, Strings.BrowsingHistoryItem, 26, enabled: true);
            AddOption(group, BrowsingDataSelection.DownloadHistory, Strings.DownloadHistoryItem, 56, enabled: profileAvailable);
            AddOption(group, BrowsingDataSelection.DiskCache, Strings.DiskCacheItem, 86, enabled: profileAvailable);
            AddOption(group, BrowsingDataSelection.Cookies, Strings.CookiesItem, 116, enabled: profileAvailable);
            AddOption(group, BrowsingDataSelection.SiteStorage, Strings.SiteStorageItem, 146, enabled: profileAvailable);
            AddOption(group, BrowsingDataSelection.Autofill, Strings.AutofillItem, 176, enabled: profileAvailable);
            AddOption(group, BrowsingDataSelection.Passwords, Strings.SavedPasswordsItem, 206, enabled: profileAvailable);

            _idleMessage = profileAvailable ? Strings.DeleteBrowsingDataWarning : Strings.ProfileDataUnavailable;
            _idleMessageColor = profileAvailable ? SystemColors.ControlText : SystemColors.GrayText;
            _messageLabel = new Label
            {
                Text = _idleMessage,
                Location = new Point(18, 332),
                Size = new Size(490, 54),
                ForeColor = _idleMessageColor
            };
            _deleteButton = new XpButton
            {
                Text = Strings.DeleteSelected,
                Location = new Point(316, 408),
                Size = new Size(92, 27)
            };
            _deleteButton.Click += async (sender, args) => await DeleteAsync().ConfigureAwait(true);
            _cancelButton = new XpButton
            {
                Text = Strings.Cancel,
                Location = new Point(416, 408),
                Size = new Size(92, 27),
                DialogResult = DialogResult.Cancel
            };

            ContentPanel.Controls.AddRange(new Control[] { intro, group, _messageLabel, _deleteButton, _cancelButton });
            AcceptButton = _deleteButton;
            CancelButton = _cancelButton;
            UpdateDeleteButton();
        }

        internal BrowsingDataSelection Selection
        {
            get
            {
                var selection = BrowsingDataSelection.None;
                foreach (var item in _items)
                {
                    if (item.Value.Checked)
                    {
                        selection |= item.Key;
                    }
                }
                return selection;
            }
        }

        private void AddOption(
            Control parent,
            BrowsingDataSelection selection,
            string text,
            int top,
            bool enabled)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                Location = new Point(20, top),
                Size = new Size(440, 24),
                Checked = BrowsingDataSelection.SafeDefaults.HasFlag(selection) && enabled,
                Enabled = enabled
            };
            checkBox.CheckedChanged += (sender, args) => UpdateDeleteButton();
            _items[selection] = checkBox;
            _availability[selection] = enabled;
            parent.Controls.Add(checkBox);
        }

        private async Task DeleteAsync()
        {
            var selection = Selection;
            if (selection == BrowsingDataSelection.None)
            {
                return;
            }

            SetBusy(true);
            try
            {
                await _deleteAction(selection).ConfigureAwait(true);
                _busy = false;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(CultureInfo.CurrentCulture, Strings.BrowsingDataDeleteFailedFormat, ex.Message),
                    Branding.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            UseWaitCursor = busy;
            foreach (var item in _items)
            {
                item.Value.Enabled = !busy && _availability[item.Key];
            }
            _messageLabel.Text = busy ? Strings.DeletingBrowsingData : _idleMessage;
            _messageLabel.ForeColor = busy ? SystemColors.ControlText : _idleMessageColor;
            _cancelButton.Enabled = !busy;
            _deleteButton.Enabled = !busy && Selection != BrowsingDataSelection.None;
        }

        private void UpdateDeleteButton()
        {
            if (_deleteButton != null)
            {
                _deleteButton.Enabled = Selection != BrowsingDataSelection.None;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs args)
        {
            if (_busy && args.CloseReason == CloseReason.UserClosing)
            {
                args.Cancel = true;
                return;
            }

            base.OnFormClosing(args);
        }
    }
}
