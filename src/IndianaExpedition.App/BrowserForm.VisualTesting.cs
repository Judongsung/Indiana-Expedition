using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Downloads;
using IndianaExpedition.Permissions;
using IndianaExpedition.ContextMenus;
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
                        sitePermissionsAvailable: true,
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.DownloadProgressDialog:
                    _visualTestDialog = new DownloadProgressDialog(
                        new VisualDownloadController(
                            DownloadTransferState.InProgress,
                            PrepareVisualDownloadFile(VisualTestConstants.DownloadFileName)),
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.DownloadCompletedDialog:
                    _visualTestDialog = new DownloadProgressDialog(
                        new VisualDownloadController(
                            DownloadTransferState.Completed,
                            PrepareVisualDownloadFile(VisualTestConstants.DownloadFileName)),
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.DownloadHistoryDialog:
                    _visualTestDialog = new DownloadHistoryDialog(
                        new VisualDownloadHistoryController(
                            PrepareVisualDownloadFile(VisualTestConstants.DownloadFileName),
                            PrepareVisualDownloadFile(VisualTestConstants.DownloadImageFileName)),
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.PermissionRequestDialog:
                    _visualTestDialog = new PermissionRequestDialog(
                        VisualTestConstants.PermissionOrigin,
                        CoreWebView2PermissionKind.Camera,
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.PrivacyTab:
                    _visualTestDialog = new InternetOptionsDialog(
                        _services.Settings,
                        BrowserDefaults.BlankPageUrl,
                        new VisualSitePermissionController(),
                        showPrivacyTab: true,
                        preventActivationOnShow: true);
                    captureTarget = _visualTestDialog;
                    break;
                case VisualTestState.ContextMenu:
                    _visualTestContextMenu = CreatePageContextMenu(
                        new PageContextMenuModel(linkUri: null, selectionText: null));
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
            _visualTestContextMenu?.Show(
                _browserHost,
                new System.Drawing.Point(
                    VisualTestConstants.ContextMenuLeft,
                    VisualTestConstants.ContextMenuTop));
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

        private string PrepareVisualDownloadFile(string fileName)
        {
            var path = Path.Combine(_services.Paths.DataDirectory, fileName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }
            return path;
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

        private sealed class VisualDownloadController : IDownloadController
        {
            private readonly string _filePath;

            internal VisualDownloadController(DownloadTransferState state, string filePath)
            {
                State = state;
                _filePath = filePath;
            }

            public event EventHandler Changed;

            public string FileName => VisualTestConstants.DownloadFileName;

            public string FilePath => _filePath;

            public string SourceHost => VisualTestConstants.DownloadSourceHost;

            public long BytesReceived => State == DownloadTransferState.Completed
                ? VisualTestConstants.DownloadTotalBytes
                : VisualTestConstants.DownloadBytesReceived;

            public long? TotalBytes => VisualTestConstants.DownloadTotalBytes;

            public DateTime? EstimatedEndTimeUtc => IsFinished
                ? (DateTime?)null
                : DateTime.UtcNow.AddMinutes(2);

            public DownloadTransferState State { get; private set; }

            public bool CanPause => State == DownloadTransferState.InProgress;

            public bool CanResume => State == DownloadTransferState.Paused;

            public bool IsFinished => State == DownloadTransferState.Completed ||
                                      State == DownloadTransferState.Canceled ||
                                      State == DownloadTransferState.Interrupted;

            public void Pause()
            {
                State = DownloadTransferState.Paused;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public void Resume()
            {
                State = DownloadTransferState.InProgress;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public void Cancel()
            {
                State = DownloadTransferState.Canceled;
                Changed?.Invoke(this, EventArgs.Empty);
            }

        }

        private sealed class VisualDownloadHistoryController : IDownloadHistoryController
        {
            private readonly List<DownloadRecord> _items;

            internal VisualDownloadHistoryController(
                string completedFilePath,
                string completedImagePath)
            {
                _items = new List<DownloadRecord>
                {
                    CreateDownloadRecord(
                        completedFilePath,
                        DownloadRecordState.Completed,
                        VisualTestConstants.DownloadTotalBytes,
                        0),
                    CreateDownloadRecord(
                        completedImagePath,
                        DownloadRecordState.Completed,
                        VisualTestConstants.DownloadImageBytes,
                        -1),
                    CreateDownloadRecord(
                        Path.Combine(
                            Path.GetDirectoryName(completedFilePath),
                            VisualTestConstants.DownloadDocumentFileName),
                        DownloadRecordState.Failed,
                        VisualTestConstants.DownloadDocumentBytes,
                        -2)
                };
            }

            public event EventHandler Changed;

            public IReadOnlyList<DownloadRecord> Items => _items.Select(item => item.Clone()).ToList();

            public bool Remove(string id)
            {
                var removed = _items.RemoveAll(item => item.Id == id) > 0;
                if (removed)
                {
                    Changed?.Invoke(this, EventArgs.Empty);
                }
                return removed;
            }

            public void Clear()
            {
                _items.Clear();
                Changed?.Invoke(this, EventArgs.Empty);
            }

            private static DownloadRecord CreateDownloadRecord(
                string filePath,
                DownloadRecordState state,
                long bytes,
                int dayOffset)
            {
                return new DownloadRecord
                {
                    Id = Path.GetFileName(filePath),
                    FileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    StartedAtUtc = DateTime.UtcNow.AddDays(dayOffset).AddMinutes(-5),
                    FinishedAtUtc = DateTime.UtcNow.AddDays(dayOffset),
                    BytesReceived = bytes,
                    TotalBytes = bytes,
                    State = state
                };
            }
        }

        private sealed class VisualSitePermissionController : ISitePermissionController
        {
            private readonly List<SitePermissionSetting> _settings = new List<SitePermissionSetting>
            {
                new SitePermissionSetting(
                    "https://maps.example.com",
                    CoreWebView2PermissionKind.Geolocation,
                    CoreWebView2PermissionState.Allow),
                new SitePermissionSetting(
                    "https://meeting.example.com",
                    CoreWebView2PermissionKind.Camera,
                    CoreWebView2PermissionState.Allow),
                new SitePermissionSetting(
                    "https://news.example.com",
                    CoreWebView2PermissionKind.Notifications,
                    CoreWebView2PermissionState.Deny)
            };

            public Task<IReadOnlyList<SitePermissionSetting>> GetSettingsAsync()
            {
                return Task.FromResult<IReadOnlyList<SitePermissionSetting>>(_settings.ToList());
            }

            public Task SetStateAsync(
                SitePermissionSetting setting,
                CoreWebView2PermissionState state)
            {
                _settings.Remove(setting);
                if (state != CoreWebView2PermissionState.Default)
                {
                    _settings.Add(new SitePermissionSetting(setting.Origin, setting.Kind, state));
                }
                return Task.CompletedTask;
            }

            public Task ResetAllAsync()
            {
                _settings.Clear();
                return Task.CompletedTask;
            }
        }
    }
}
