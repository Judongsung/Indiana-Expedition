using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Dialogs
{
    internal enum RuntimeRequirementState
    {
        InstallRequired,
        UpdateRequired
    }

    internal sealed class RuntimeMissingDialog : LunaForm
    {
        internal RuntimeMissingDialog(RuntimeRequirementState state, string detectedVersion)
        {
            var updateRequired = state == RuntimeRequirementState.UpdateRequired;
            Text = updateRequired ? Strings.RuntimeUpdateTitle : Strings.RuntimeMissingTitle;
            SetContentClientSize(476, 174);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var message = new Label
            {
                AutoSize = false,
                Text = updateRequired
                    ? string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.RuntimeUpdateMessageFormat,
                        detectedVersion,
                        WebViewRuntimeConstants.MinimumVersion)
                    : string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.RuntimeMissingMessageFormat,
                        WebViewRuntimeConstants.MinimumVersion),
                Location = new Point(24, 22),
                Size = new Size(428, 66)
            };

            var link = new LinkLabel
            {
                AutoSize = true,
                Text = Strings.RuntimeDownload,
                Location = new Point(24, 96)
            };
            link.LinkClicked += (sender, args) => OpenDownloadPage();

            var close = new XpButton
            {
                Text = Strings.Ok,
                DialogResult = DialogResult.OK,
                Location = new Point(373, 132),
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
