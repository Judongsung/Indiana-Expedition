using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Dialogs
{
    internal sealed class AboutDialog : LunaForm
    {
        internal AboutDialog(bool preventActivationOnShow = false)
        {
            PreventActivationOnShow = preventActivationOnShow;
            Text = string.Format(CultureInfo.CurrentCulture, Strings.AboutTitleFormat, Branding.ProductName);
            SetContentClientSize(500, 286);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = preventActivationOnShow;
            StartPosition = preventActivationOnShow
                ? FormStartPosition.CenterScreen
                : FormStartPosition.CenterParent;

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
            reference.LinkClicked += (sender, args) => OpenUrl(ApplicationConstants.WebView2BrowserProjectUrl);

            var repositoryLabel = new Label
            {
                Text = Strings.AboutRepository,
                AutoSize = true,
                Location = new Point(24, 180)
            };
            var repository = new LinkLabel
            {
                Text = ApplicationConstants.ProjectRepositoryUrl,
                AutoSize = true,
                Location = new Point(128, 180)
            };
            repository.LinkClicked += (sender, args) => OpenUrl(ApplicationConstants.ProjectRepositoryUrl);

            var ok = new XpButton
            {
                Text = Strings.Ok,
                DialogResult = DialogResult.OK,
                Location = new Point(398, 242),
                Size = new Size(78, 27)
            };
            ContentPanel.Controls.AddRange(new Control[]
            {
                logo,
                name,
                description,
                repositoryLabel,
                repository,
                reference,
                ok
            });
            AcceptButton = ok;
            CancelButton = ok;
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
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
