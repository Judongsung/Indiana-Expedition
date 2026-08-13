using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Downloads;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed class DownloadProgressDialog : LunaForm
    {
        private readonly IDownloadController _controller;
        private readonly Label _stateLabel;
        private readonly Label _progressLabel;
        private readonly Label _estimateLabel;
        private readonly ProgressBar _progressBar;
        private readonly XpButton _pauseResumeButton;
        private readonly XpButton _cancelButton;
        private readonly XpButton _openButton;
        private readonly XpButton _openFolderButton;
        private readonly XpButton _closeButton;
        private bool _allowClose;

        internal DownloadProgressDialog(
            IDownloadController controller,
            bool preventActivationOnShow = false)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            PreventActivationOnShow = preventActivationOnShow;
            Text = Strings.DownloadProgressTitle;
            SetContentClientSize(520, 270);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = preventActivationOnShow;
            StartPosition = preventActivationOnShow
                ? FormStartPosition.CenterScreen
                : FormStartPosition.CenterParent;

            var fileName = new Label
            {
                Text = controller.FileName,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(22, 20),
                Size = new Size(476, 24),
                AutoEllipsis = true
            };
            var source = new Label
            {
                Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadSourceFormat,
                    controller.SourceHost),
                Location = new Point(22, 50),
                Size = new Size(476, 22),
                AutoEllipsis = true
            };
            var destination = new Label
            {
                Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadDestinationFormat,
                    controller.FilePath),
                Location = new Point(22, 74),
                Size = new Size(476, 38),
                AutoEllipsis = true
            };
            _stateLabel = new Label
            {
                Location = new Point(22, 116),
                Size = new Size(476, 22)
            };
            _progressBar = new ProgressBar
            {
                Location = new Point(22, 142),
                Size = new Size(476, 22)
            };
            _progressLabel = new Label
            {
                Location = new Point(22, 170),
                Size = new Size(250, 22)
            };
            _estimateLabel = new Label
            {
                Location = new Point(274, 170),
                Size = new Size(224, 22),
                TextAlign = ContentAlignment.TopRight
            };

            _pauseResumeButton = new XpButton
            {
                Location = new Point(82, 218),
                Size = new Size(82, 27)
            };
            _pauseResumeButton.Click += OnPauseResumeClicked;
            _cancelButton = new XpButton
            {
                Text = Strings.Cancel,
                Location = new Point(172, 218),
                Size = new Size(82, 27)
            };
            _cancelButton.Click += (sender, args) => CancelDownload();
            _openButton = new XpButton
            {
                Text = Strings.OpenDownloadedFile,
                Location = new Point(172, 218),
                Size = new Size(98, 27)
            };
            _openButton.Click += (sender, args) => OpenPath(_controller.FilePath);
            _openFolderButton = new XpButton
            {
                Text = Strings.OpenDownloadFolder,
                Location = new Point(278, 218),
                Size = new Size(112, 27)
            };
            _openFolderButton.Click += (sender, args) => OpenFolder();
            _closeButton = new XpButton
            {
                Text = Strings.CloseButton,
                Location = new Point(398, 218),
                Size = new Size(100, 27)
            };
            _closeButton.Click += (sender, args) =>
            {
                _allowClose = true;
                Close();
            };

            ContentPanel.Controls.AddRange(new Control[]
            {
                fileName,
                source,
                destination,
                _stateLabel,
                _progressBar,
                _progressLabel,
                _estimateLabel,
                _pauseResumeButton,
                _cancelButton,
                _openButton,
                _openFolderButton,
                _closeButton
            });

            _controller.Changed += OnControllerChanged;
            UpdateView();
        }

        protected override void OnFormClosing(FormClosingEventArgs args)
        {
            if (_allowClose || _controller.IsFinished)
            {
                base.OnFormClosing(args);
                return;
            }

            if (args.CloseReason == CloseReason.FormOwnerClosing ||
                args.CloseReason == CloseReason.ApplicationExitCall ||
                args.CloseReason == CloseReason.WindowsShutDown ||
                args.CloseReason == CloseReason.TaskManagerClosing)
            {
                _allowClose = true;
                _controller.Cancel();
                base.OnFormClosing(args);
                return;
            }

            if (!LunaConfirmationDialog.Confirm(
                this,
                Strings.CancelDownloadTitle,
                Strings.CancelDownloadPrompt,
                Strings.CancelDownload))
            {
                args.Cancel = true;
                return;
            }

            _allowClose = true;
            _controller.Cancel();
            base.OnFormClosing(args);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _controller.Changed -= OnControllerChanged;
            }
            base.Dispose(disposing);
        }

        private void OnControllerChanged(object sender, EventArgs args)
        {
            if (IsDisposed)
            {
                return;
            }
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateView));
                return;
            }
            UpdateView();
        }

        private void UpdateView()
        {
            _stateLabel.Text = DownloadDisplayFormatter.FormatState(_controller.State);
            UpdateProgress();

            _pauseResumeButton.Text = _controller.CanResume ? Strings.ResumeDownload : Strings.PauseDownload;
            _pauseResumeButton.Enabled = _controller.CanPause || _controller.CanResume;

            var completed = _controller.State == DownloadTransferState.Completed;
            var active = !_controller.IsFinished;
            _pauseResumeButton.Visible = active;
            _cancelButton.Visible = active;
            _openButton.Visible = !active;
            _openFolderButton.Visible = !active;
            _closeButton.Visible = !active;
            _openButton.Enabled = completed && File.Exists(_controller.FilePath);
            _openFolderButton.Enabled = Directory.Exists(Path.GetDirectoryName(_controller.FilePath));
        }

        private void UpdateProgress()
        {
            if (_controller.TotalBytes.HasValue)
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
                var total = Math.Max(1L, _controller.TotalBytes.Value);
                _progressBar.Value = (int)Math.Min(
                    DownloadUiConstants.ProgressMaximum,
                    (_controller.BytesReceived * (double)DownloadUiConstants.ProgressMaximum) / total);
                _progressLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadProgressFormat,
                    DownloadDisplayFormatter.FormatBytes(_controller.BytesReceived),
                    DownloadDisplayFormatter.FormatBytes(total));
            }
            else
            {
                _progressBar.Style = _controller.IsFinished
                    ? ProgressBarStyle.Continuous
                    : ProgressBarStyle.Marquee;
                _progressBar.Value = _controller.State == DownloadTransferState.Completed
                    ? DownloadUiConstants.ProgressMaximum
                    : 0;
                _progressLabel.Text = DownloadDisplayFormatter.FormatBytes(_controller.BytesReceived);
            }

            _estimateLabel.Text = _controller.EstimatedEndTimeUtc.HasValue && !_controller.IsFinished
                ? string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadEstimatedEndFormat,
                    _controller.EstimatedEndTimeUtc.Value.ToLocalTime())
                : string.Empty;
        }

        private void OnPauseResumeClicked(object sender, EventArgs args)
        {
            if (_controller.CanResume)
            {
                _controller.Resume();
            }
            else
            {
                _controller.Pause();
            }
        }

        private void CancelDownload()
        {
            if (LunaConfirmationDialog.Confirm(
                this,
                Strings.CancelDownloadTitle,
                Strings.CancelDownloadPrompt,
                Strings.CancelDownload))
            {
                _controller.Cancel();
            }
        }

        private void OpenFolder()
        {
            OpenPath(Path.GetDirectoryName(_controller.FilePath));
        }

        private static void OpenPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
