using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed class RuntimeMissingDialog : LunaForm
    {
        internal RuntimeMissingDialog()
        {
            Text = Strings.RuntimeMissingTitle;
            SetContentClientSize(450, 150);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var message = new Label
            {
                AutoSize = false,
                Text = Strings.RuntimeMissingMessage,
                Location = new Point(24, 22),
                Size = new Size(400, 42)
            };

            var link = new LinkLabel
            {
                AutoSize = true,
                Text = Strings.RuntimeDownload,
                Location = new Point(24, 72)
            };
            link.LinkClicked += (sender, args) => OpenDownloadPage();

            var close = new XpButton
            {
                Text = Strings.Ok,
                DialogResult = DialogResult.OK,
                Location = new Point(347, 108),
                Size = new Size(78, 26)
            };

            ContentPanel.Controls.Add(message);
            ContentPanel.Controls.Add(link);
            ContentPanel.Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;
        }

        private static void OpenDownloadPage()
        {
            Process.Start(new ProcessStartInfo(ApplicationConstants.WebView2DownloadUrl) { UseShellExecute = true });
        }
    }
}
