using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndianaExpedition.Constants;
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

            Form captureTarget = this;
            switch (_visualTestState)
            {
                case VisualTestState.PopupBlocked:
                    EnqueueBlockedPopup(
                        VisualTestConstants.PopupSourceOrigin,
                        VisualTestConstants.PopupTargetUrl);
                    break;
                case VisualTestState.FindDialog:
                    _visualTestFindController = new VisualPageFindController(
                        VisualTestConstants.FindActiveMatchIndex,
                        VisualTestConstants.FindMatchCount);
                    _visualTestDialog = new PageFindDialog(
                        _visualTestFindController,
                        new PageFindCriteria { Term = VisualTestConstants.FindTerm },
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.DeleteBrowsingDataDialog:
                    _visualTestDialog = new DeleteBrowsingDataDialog(
                        selection => Task.CompletedTask,
                        profileAvailable: true,
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.HelpMenu:
                    _helpMenu.ShowDropDown();
                    break;
                case VisualTestState.AboutDialog:
                    _visualTestDialog = new AboutDialog(preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
            }

            PerformLayout();
            Invalidate(true);
            Update();
            if (!ReferenceEquals(captureTarget, this))
            {
                captureTarget.Show();
                captureTarget.SendToBack();
                captureTarget.PerformLayout();
                captureTarget.Invalidate(true);
                captureTarget.Update();
            }
            Application.DoEvents();
            SignalVisualTestReady(captureTarget);
        }

        private void SignalVisualTestReady(Form captureTarget)
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

            File.WriteAllText(
                readyFile,
                captureTarget.Handle.ToInt64().ToString(CultureInfo.InvariantCulture));
        }

        private sealed class VisualPageFindController : IPageFindController
        {
            private PageFindCriteria _criteria;

            internal VisualPageFindController(int activeMatchIndex, int matchCount)
            {
                ActiveMatchIndex = activeMatchIndex;
                MatchCount = matchCount;
            }

            public event EventHandler StateChanged;

            public int ActiveMatchIndex { get; private set; }

            public int MatchCount { get; }

            public PageFindCriteria CurrentCriteria => _criteria?.Clone();

            public Task FindAsync(PageFindCriteria criteria)
            {
                _criteria = criteria?.Clone();
                StateChanged?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            }

            public Task RepeatAsync(bool previous)
            {
                StateChanged?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            }

            public void ResetSession()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
