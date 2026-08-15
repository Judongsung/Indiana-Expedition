using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.BrowsingData;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Dialogs;
using IndianaExpedition.Downloads;
using IndianaExpedition.Find;
using IndianaExpedition.Permissions;
using IndianaExpedition.Settings;
using IndianaExpedition.VisualTesting;

namespace IndianaExpedition.VisualTestHost
{
    internal sealed class VisualTestScenario : IVisualTestScenario
    {
        private readonly string _state;

        internal VisualTestScenario(string state)
        {
            _state = state;
        }

        public void Prepare(IVisualTestSurface surface)
        {
            surface.Reset();
            var captureTarget = surface.ContextMenuOwner.FindForm();
            ContextMenuStrip contextMenu = null;
            IPageFindController findController = null;
            switch (_state.ToLowerInvariant())
            {
                case "favorites":
                    surface.ShowFavorites();
                    break;
                case "history":
                    surface.ShowHistory();
                    break;
                case "popupblocked":
                    surface.ShowBlockedPopup(VisualFixture.PopupSourceOrigin, VisualFixture.PopupTargetUrl);
                    break;
                case "finddialog":
                    findController = new StubPageFindController(2, 5);
                    captureTarget = new PageFindDialog(
                        findController,
                        new PageFindCriteria { Term = VisualFixture.FindTerm },
                        preventActivationOnShow: true);
                    break;
                case "deletebrowsingdatadialog":
                    captureTarget = new DeleteBrowsingDataDialog(
                        selection => Task.CompletedTask,
                        profileAvailable: true,
                        sitePermissionsAvailable: true,
                        preventActivationOnShow: true);
                    break;
                case "downloadprogressdialog":
                    captureTarget = new DownloadProgressDialog(
                        new StubDownloadController(
                            DownloadTransferState.InProgress,
                            surface.PrepareDataFile(VisualFixture.DownloadFileName)),
                        preventActivationOnShow: true,
                        externalLauncher: NoOpExternalLauncher.Instance);
                    break;
                case "downloadcompleteddialog":
                    captureTarget = new DownloadProgressDialog(
                        new StubDownloadController(
                            DownloadTransferState.Completed,
                            surface.PrepareDataFile(VisualFixture.DownloadFileName)),
                        preventActivationOnShow: true,
                        externalLauncher: NoOpExternalLauncher.Instance);
                    break;
                case "downloadhistorydialog":
                    captureTarget = new DownloadHistoryDialog(
                        new StubDownloadHistoryController(
                            surface.PrepareDataFile(VisualFixture.DownloadFileName),
                            surface.PrepareDataFile(VisualFixture.DownloadImageFileName)),
                        preventActivationOnShow: true,
                        externalLauncher: NoOpExternalLauncher.Instance);
                    break;
                case "permissionrequestdialog":
                    captureTarget = new PermissionRequestDialog(
                        VisualFixture.PermissionOrigin,
                        CoreWebView2PermissionKind.Camera,
                        preventActivationOnShow: true);
                    break;
                case "privacytab":
                    captureTarget = new InternetOptionsDialog(
                        surface.Services.Settings,
                        BrowserDefaults.BlankPageUrl,
                        new StubSitePermissionController(),
                        showPrivacyTab: true,
                        preventActivationOnShow: true);
                    break;
                case "contextmenu":
                    contextMenu = surface.CreateContextMenu(
                        new PageContextMenuModel(linkUri: null, selectionText: null));
                    break;
                case "helpmenu":
                    surface.ShowHelpMenu();
                    break;
                case "aboutdialog":
                    captureTarget = new AboutDialog(
                        preventActivationOnShow: true,
                        externalLauncher: NoOpExternalLauncher.Instance);
                    break;
            }
            surface.Present(
                captureTarget,
                contextMenu,
                findController,
                contextMenu == null
                    ? (Point?)null
                    : new Point(VisualFixture.ContextMenuLeft, VisualFixture.ContextMenuTop));
        }

        private static class VisualFixture
        {
            internal const string PopupSourceOrigin = "https://example.com";
            internal const string PopupTargetUrl = "https://example.com/popup";
            internal const string FindTerm = "Windows XP";
            internal const string DownloadFileName = "WindowsXP-KB-demo.exe";
            internal const string DownloadImageFileName = "luna-theme-reference.png";
            internal const string DownloadDocumentFileName = "legacy-browser-notes.pdf";
            internal const string DownloadSourceHost = "download.microsoft.com";
            internal const long DownloadBytesReceived = 7340032L;
            internal const long DownloadTotalBytes = 10485760L;
            internal const long DownloadImageBytes = 2457600L;
            internal const long DownloadDocumentBytes = 786432L;
            internal const string PermissionOrigin = "https://example.com";
            internal const int ContextMenuLeft = 310;
            internal const int ContextMenuTop = 160;
        }

        private sealed class StubPageFindController : IPageFindController
        {
            private PageFindCriteria _criteria;
            internal StubPageFindController(int activeMatchIndex, int matchCount)
            {
                ActiveMatchIndex = activeMatchIndex;
                MatchCount = matchCount;
            }
            public event EventHandler StateChanged;
            public int ActiveMatchIndex { get; }
            public int MatchCount { get; }
            public PageFindCriteria CurrentCriteria => _criteria?.Clone();
            public Task FindAsync(PageFindCriteria criteria)
            {
                _criteria = criteria?.Clone();
                StateChanged?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            }
            public Task RepeatAsync(bool previous) => Task.CompletedTask;
            public void ResetSession() { }
            public void Dispose() { }
        }

        private sealed class StubDownloadController : IDownloadController
        {
            internal StubDownloadController(DownloadTransferState state, string filePath)
            {
                State = state;
                FilePath = filePath;
            }
            public event EventHandler Changed;
            public string FileName => VisualFixture.DownloadFileName;
            public string FilePath { get; }
            public string SourceHost => VisualFixture.DownloadSourceHost;
            public long BytesReceived => State == DownloadTransferState.Completed
                ? VisualFixture.DownloadTotalBytes
                : VisualFixture.DownloadBytesReceived;
            public long? TotalBytes => VisualFixture.DownloadTotalBytes;
            public DateTime? EstimatedEndTimeUtc => IsFinished ? (DateTime?)null : DateTime.UtcNow.AddMinutes(2);
            public DownloadTransferState State { get; private set; }
            public bool CanPause => State == DownloadTransferState.InProgress;
            public bool CanResume => State == DownloadTransferState.Paused;
            public bool IsFinished => State == DownloadTransferState.Completed ||
                                      State == DownloadTransferState.Canceled ||
                                      State == DownloadTransferState.Interrupted;
            public void Pause() { State = DownloadTransferState.Paused; Changed?.Invoke(this, EventArgs.Empty); }
            public void Resume() { State = DownloadTransferState.InProgress; Changed?.Invoke(this, EventArgs.Empty); }
            public void Cancel() { State = DownloadTransferState.Canceled; Changed?.Invoke(this, EventArgs.Empty); }
        }

        private sealed class StubDownloadHistoryController : IDownloadHistoryController
        {
            private readonly List<DownloadRecord> _items;
            internal StubDownloadHistoryController(string completedFilePath, string completedImagePath)
            {
                _items = new List<DownloadRecord>
                {
                    Create(completedFilePath, DownloadRecordState.Completed, VisualFixture.DownloadTotalBytes, 0),
                    Create(completedImagePath, DownloadRecordState.Completed, VisualFixture.DownloadImageBytes, -1),
                    Create(Path.Combine(Path.GetDirectoryName(completedFilePath), VisualFixture.DownloadDocumentFileName), DownloadRecordState.Failed, VisualFixture.DownloadDocumentBytes, -2)
                };
            }
            public event EventHandler Changed;
            public IReadOnlyList<DownloadRecord> Items => _items.Select(item => item.Clone()).ToList();
            public bool Remove(string id)
            {
                var removed = _items.RemoveAll(item => item.Id == id) > 0;
                if (removed) Changed?.Invoke(this, EventArgs.Empty);
                return removed;
            }
            public void Clear() { _items.Clear(); Changed?.Invoke(this, EventArgs.Empty); }
            private static DownloadRecord Create(string path, DownloadRecordState state, long bytes, int dayOffset)
            {
                return new DownloadRecord
                {
                    Id = Path.GetFileName(path), FileName = Path.GetFileName(path), FilePath = path,
                    StartedAtUtc = DateTime.UtcNow.AddDays(dayOffset).AddMinutes(-5),
                    FinishedAtUtc = DateTime.UtcNow.AddDays(dayOffset), BytesReceived = bytes,
                    TotalBytes = bytes, State = state
                };
            }
        }

        private sealed class StubSitePermissionController : ISitePermissionController
        {
            private readonly List<SitePermissionSetting> _settings = new List<SitePermissionSetting>
            {
                new SitePermissionSetting("https://maps.example.com", CoreWebView2PermissionKind.Geolocation, CoreWebView2PermissionState.Allow),
                new SitePermissionSetting("https://meeting.example.com", CoreWebView2PermissionKind.Camera, CoreWebView2PermissionState.Allow),
                new SitePermissionSetting("https://news.example.com", CoreWebView2PermissionKind.Notifications, CoreWebView2PermissionState.Deny)
            };
            public Task<IReadOnlyList<SitePermissionSetting>> GetSettingsAsync() =>
                Task.FromResult<IReadOnlyList<SitePermissionSetting>>(_settings.ToList());
            public Task SetStateAsync(SitePermissionSetting setting, CoreWebView2PermissionState state)
            {
                _settings.Remove(setting);
                if (state != CoreWebView2PermissionState.Default)
                    _settings.Add(new SitePermissionSetting(setting.Origin, setting.Kind, state));
                return Task.CompletedTask;
            }
            public Task ResetAllAsync() { _settings.Clear(); return Task.CompletedTask; }
        }
    }
}
