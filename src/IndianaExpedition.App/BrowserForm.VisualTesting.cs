using System;
using System.IO;
using System.Windows.Forms;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Resources;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private void PrepareVisualTestSurface()
        {
            _browserHost.Controls.Clear();
            _addressBox.Text = BrowserDefaults.BlankPageUrl;
            _statusLabel.Text = Strings.Ready;
            _progressBar.Visible = false;
            _stopButton.Enabled = false;
            _backButton.Enabled = false;
            _forwardButton.Enabled = false;

            PerformLayout();
            Invalidate(true);
            Update();
            Application.DoEvents();
            SignalVisualTestReady();
        }

        private void SignalVisualTestReady()
        {
            if (string.IsNullOrWhiteSpace(_visualTestReadyFile))
            {
                return;
            }

            var readyFile = Path.GetFullPath(_visualTestReadyFile);
            var directory = Path.GetDirectoryName(readyFile);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(readyFile, string.Empty);
        }
    }
}
