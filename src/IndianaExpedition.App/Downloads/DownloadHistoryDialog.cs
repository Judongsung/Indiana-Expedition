using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Dialogs;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;
using IndianaExpedition.Constants;
using IndianaExpedition.Commands;

namespace IndianaExpedition.Downloads
{
    internal sealed class DownloadHistoryDialog : LunaForm
    {
        private readonly IDownloadHistoryController _controller;
        private readonly IExternalLauncher _externalLauncher;
        private readonly ListView _list;
        private readonly Label _emptyLabel;
        private readonly XpButton _openButton;
        private readonly XpButton _openFolderButton;
        private readonly XpButton _removeButton;
        private readonly XpButton _clearButton;

        internal DownloadHistoryDialog(
            IDownloadHistoryController controller,
            bool preventActivationOnShow = false,
            IExternalLauncher externalLauncher = null)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _externalLauncher = externalLauncher ?? new ShellExternalLauncher();
            PreventActivationOnShow = preventActivationOnShow;
            Text = Strings.DownloadHistoryTitle;
            SetContentClientSize(720, 410);
            LunaResizable = true;
            MaximizeBox = true;
            MinimizeBox = true;
            ShowInTaskbar = preventActivationOnShow;
            StartPosition = preventActivationOnShow
                ? FormStartPosition.CenterScreen
                : FormStartPosition.CenterParent;

            _list = new ListView
            {
                Location = new Point(16, 18),
                Size = new Size(688, 310),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true
            };
            _list.Columns.Add(Strings.DownloadFileColumn, 170);
            _list.Columns.Add(Strings.DownloadStatusColumn, 72);
            _list.Columns.Add(Strings.DownloadSizeColumn, 88);
            _list.Columns.Add(Strings.DownloadDateColumn, 128);
            _list.Columns.Add(Strings.DownloadLocationColumn, 220);
            _list.SelectedIndexChanged += (sender, args) => UpdateButtons();
            _list.DoubleClick += (sender, args) => OpenSelectedFile();

            _emptyLabel = new Label
            {
                Text = Strings.NoDownloadHistory,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(30, 40)
            };

            _openButton = CreateButton(Strings.OpenDownloadedFile, 16, OpenSelectedFile);
            _openFolderButton = CreateButton(Strings.OpenDownloadFolder, 122, OpenSelectedFolder);
            _removeButton = CreateButton(Strings.RemoveDownloadRecord, 242, RemoveSelected);
            _clearButton = CreateButton(Strings.ClearDownloadHistory, 362, ClearHistory);
            var close = new XpButton
            {
                Name = UiAutomationIds.DownloadHistory.CloseButton,
                Text = Strings.CloseButton,
                Location = new Point(606, 354),
                Size = new Size(98, 27),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            close.Click += OnCloseClicked;

            ContentPanel.Controls.AddRange(new Control[]
            {
                _list,
                _emptyLabel,
                _openButton,
                _openFolderButton,
                _removeButton,
                _clearButton,
                close
            });
            CancelButton = close;
            _controller.Changed += OnHistoryChanged;
            RefreshList();
        }

        private void OnCloseClicked(object sender, EventArgs args)
        {
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _controller.Changed -= OnHistoryChanged;
            }
            base.Dispose(disposing);
        }

        private XpButton CreateButton(string text, int left, Action action)
        {
            var button = new XpButton
            {
                Text = text,
                Location = new Point(left, 354),
                Size = new Size(106, 27),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            button.Click += (sender, args) => action();
            return button;
        }

        private void OnHistoryChanged(object sender, EventArgs args)
        {
            if (IsDisposed)
            {
                return;
            }
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshList));
                return;
            }
            RefreshList();
        }

        private void RefreshList()
        {
            _list.BeginUpdate();
            try
            {
                _list.Items.Clear();
                foreach (var record in _controller.Items)
                {
                    var item = new ListViewItem(record.FileName) { Tag = record };
                    item.SubItems.Add(DownloadDisplayFormatter.FormatState(record.State));
                    item.SubItems.Add(
                        record.TotalBytes.HasValue
                            ? DownloadDisplayFormatter.FormatBytes(record.TotalBytes.Value)
                            : DownloadDisplayFormatter.FormatBytes(record.BytesReceived));
                    item.SubItems.Add(record.FinishedAtUtc.ToLocalTime().ToString(
                        DownloadUiConstants.HistoryDateFormat,
                        CultureInfo.CurrentCulture));
                    item.SubItems.Add(record.FilePath);
                    _list.Items.Add(item);
                }
            }
            finally
            {
                _list.EndUpdate();
            }

            _emptyLabel.Visible = _list.Items.Count == 0;
            _emptyLabel.BringToFront();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            var selected = SelectedRecords().ToList();
            var single = selected.Count == 1 ? selected[0] : null;
            _openButton.Enabled = single != null &&
                                  single.State == DownloadRecordState.Completed &&
                                  File.Exists(single.FilePath);
            _openFolderButton.Enabled = single != null &&
                                        Directory.Exists(Path.GetDirectoryName(single.FilePath));
            _removeButton.Enabled = selected.Count > 0;
            _clearButton.Enabled = _list.Items.Count > 0;
        }

        private System.Collections.Generic.IEnumerable<DownloadRecord> SelectedRecords()
        {
            return _list.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag as DownloadRecord)
                .Where(record => record != null);
        }

        private void OpenSelectedFile()
        {
            var record = SelectedRecords().FirstOrDefault();
            if (record != null &&
                record.State == DownloadRecordState.Completed &&
                File.Exists(record.FilePath))
            {
                OpenPath(record.FilePath);
            }
        }

        private void OpenSelectedFolder()
        {
            var record = SelectedRecords().FirstOrDefault();
            if (record != null)
            {
                OpenPath(Path.GetDirectoryName(record.FilePath));
            }
        }

        private void RemoveSelected()
        {
            foreach (var record in SelectedRecords().ToList())
            {
                _controller.Remove(record.Id);
            }
        }

        private void ClearHistory()
        {
            if (LunaConfirmationDialog.Confirm(
                this,
                Strings.ClearDownloadHistoryTitle,
                Strings.ClearDownloadHistoryPrompt,
                Strings.ClearDownloadHistory))
            {
                _controller.Clear();
            }
        }

        private void OpenPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                (!File.Exists(path) && !Directory.Exists(path)))
            {
                return;
            }

            try
            {
                _externalLauncher.Open(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
