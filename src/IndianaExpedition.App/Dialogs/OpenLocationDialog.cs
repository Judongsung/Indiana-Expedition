using System;
using System.Drawing;
using System.Windows.Forms;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Dialogs
{
    internal sealed class OpenLocationDialog : LunaForm
    {
        private readonly TextBox _targetBox;

        internal OpenLocationDialog(string initialValue)
        {
            Text = Strings.OpenLocationTitle;
            SetContentClientSize(500, 142);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            var label = new Label
            {
                AutoSize = true,
                Text = Strings.OpenLocationPrompt,
                Location = new Point(18, 18)
            };
            _targetBox = new TextBox
            {
                Text = initialValue ?? string.Empty,
                Location = new Point(20, 48),
                Size = new Size(370, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var browse = new XpButton
            {
                Text = Strings.BrowseMnemonic,
                Location = new Point(398, 47),
                Size = new Size(82, 25)
            };
            browse.Click += (sender, args) => BrowseForFile();

            var ok = new XpButton
            {
                Text = Strings.Ok,
                DialogResult = DialogResult.OK,
                Location = new Point(316, 100),
                Size = new Size(78, 26)
            };
            var cancel = new XpButton
            {
                Text = Strings.Cancel,
                DialogResult = DialogResult.Cancel,
                Location = new Point(402, 100),
                Size = new Size(78, 26)
            };

            ContentPanel.Controls.AddRange(new Control[] { label, _targetBox, browse, ok, cancel });
            AcceptButton = ok;
            CancelButton = cancel;
        }

        internal string Target => _targetBox.Text.Trim();

        internal bool IsLocalFile => Uri.TryCreate(Target, UriKind.Absolute, out var uri) && uri.IsFile;

        private void BrowseForFile()
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = Strings.WebPageFileFilter,
                CheckFileExists = true,
                Multiselect = false,
                Title = Strings.OpenWebPageTitle
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _targetBox.Text = new Uri(dialog.FileName).AbsoluteUri;
                }
            }
        }
    }
}
