using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;
using IndianaExpedition.Commands;

namespace IndianaExpedition.Dialogs
{
    internal sealed class AboutDialog : LunaForm
    {
        private readonly Image _logoImage;
        private readonly Font _titleFont;
        private readonly IExternalLauncher _externalLauncher;

        internal AboutDialog(
            bool preventActivationOnShow = false,
            IExternalLauncher externalLauncher = null)
        {
            _externalLauncher = externalLauncher ?? new ShellExternalLauncher();
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

            _logoImage = XpGlyphs.Create(GlyphKind.Globe, 64);
            var logo = new PictureBox
            {
                Image = _logoImage,
                Location = new Point(24, 24),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            _titleFont = new Font(Font.FontFamily, 18f, FontStyle.Bold);
            var name = new Label
            {
                Text = Branding.ProductName,
                Font = _titleFont,
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
                _externalLauncher.Open(ApplicationConstants.WebView2BrowserProjectUrl);

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
            repository.LinkClicked += (sender, args) =>
                _externalLauncher.Open(ApplicationConstants.ProjectRepositoryUrl);

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Control control in ContentPanel.Controls)
                {
                    if (control is PictureBox picture)
                    {
                        picture.Image = null;
                    }
                }
                _logoImage.Dispose();
                _titleFont.Dispose();
            }
            base.Dispose(disposing);
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
