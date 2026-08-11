using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed class AboutDialog : LunaForm
    {
        internal AboutDialog()
        {
            Text = string.Format(CultureInfo.CurrentCulture, Strings.AboutTitleFormat, Branding.ProductName);
            SetContentClientSize(500, 286);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            var logo = new PictureBox
            {
                Image = XpGlyphs.Create(GlyphKind.Globe, 64),
                Location = new Point(24, 24),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            var name = new Label
            {
                Text = Branding.ProductName,
                Font = new Font(Font.FontFamily, 18f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(108, 24)
            };
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var description = new Label
            {
                Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.AboutDescriptionFormat,
                    version,
                    SafeRuntimeVersion()),
                AutoSize = true,
                Location = new Point(110, 62)
            };
            var reference = new LinkLabel
            {
                Text = Strings.AboutReference,
                AutoSize = true,
                Location = new Point(24, 206)
            };
            reference.LinkClicked += (sender, args) =>
                Process.Start(new ProcessStartInfo(ApplicationConstants.WebView2BrowserProjectUrl) { UseShellExecute = true });

            var ok = new XpButton
            {
                Text = Strings.Ok,
                DialogResult = DialogResult.OK,
                Location = new Point(398, 242),
                Size = new Size(78, 27)
            };
            ContentPanel.Controls.AddRange(new Control[] { logo, name, description, reference, ok });
            AcceptButton = ok;
            CancelButton = ok;
        }

        private static string SafeRuntimeVersion()
        {
            try
            {
                return CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch
            {
                return Strings.RuntimeVersionUnavailable;
            }
        }
    }
}
