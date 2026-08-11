using System.Drawing;
using System.Windows.Forms;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed class TextPromptDialog : LunaForm
    {
        private readonly TextBox _textBox;

        internal TextPromptDialog(string title, string prompt, string initialValue)
        {
            Text = title;
            SetContentClientSize(390, 130);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            ContentPanel.Controls.Add(new Label { Text = prompt, AutoSize = true, Location = new Point(16, 18) });
            _textBox = new TextBox { Text = initialValue ?? string.Empty, Location = new Point(18, 45), Size = new Size(352, 23) };
            var ok = new XpButton { Text = Strings.Ok, DialogResult = DialogResult.OK, Location = new Point(206, 88), Size = new Size(78, 26) };
            var cancel = new XpButton { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, Location = new Point(292, 88), Size = new Size(78, 26) };
            ContentPanel.Controls.AddRange(new Control[] { _textBox, ok, cancel });
            AcceptButton = ok;
            CancelButton = cancel;

            Shown += (sender, args) =>
            {
                _textBox.Focus();
                _textBox.SelectAll();
            };
        }

        internal string Value => _textBox.Text.Trim();
    }
}
