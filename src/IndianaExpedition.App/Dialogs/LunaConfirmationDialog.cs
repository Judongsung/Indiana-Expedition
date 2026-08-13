using System.Drawing;
using System.Windows.Forms;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Dialogs
{
    internal sealed class LunaConfirmationDialog : LunaForm
    {
        private LunaConfirmationDialog(
            string title,
            string message,
            string confirmText)
        {
            Text = title;
            SetContentClientSize(430, 170);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            var messageLabel = new Label
            {
                Text = message,
                Location = new Point(22, 24),
                Size = new Size(386, 82)
            };
            var confirm = new XpButton
            {
                Text = confirmText,
                DialogResult = DialogResult.OK,
                Location = new Point(254, 126),
                Size = new Size(76, 27)
            };
            var cancel = new XpButton
            {
                Text = Strings.Cancel,
                DialogResult = DialogResult.Cancel,
                Location = new Point(338, 126),
                Size = new Size(76, 27)
            };

            ContentPanel.Controls.AddRange(new Control[] { messageLabel, confirm, cancel });
            AcceptButton = confirm;
            CancelButton = cancel;
        }

        internal static bool Confirm(
            IWin32Window owner,
            string title,
            string message,
            string confirmText)
        {
            using (var dialog = new LunaConfirmationDialog(title, message, confirmText))
            {
                return dialog.ShowDialog(owner) == DialogResult.OK;
            }
        }
    }
}
