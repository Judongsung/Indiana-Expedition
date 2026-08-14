using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndianaExpedition.BrowsingData;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Downloads;
using IndianaExpedition.Find;

namespace IndianaExpedition.App.Tests
{
    internal static class DialogInteractionTests
    {
        private static readonly BrowsingDataSelection[] BrowsingDataOptions =
        {
            BrowsingDataSelection.History,
            BrowsingDataSelection.DownloadHistory,
            BrowsingDataSelection.DiskCache,
            BrowsingDataSelection.Cookies,
            BrowsingDataSelection.SiteStorage,
            BrowsingDataSelection.Autofill,
            BrowsingDataSelection.Passwords,
            BrowsingDataSelection.SitePermissions
        };

        internal static void DownloadHistoryCloseButton(TestContext context)
        {
            var controller = new RecordingDownloadHistoryController();
            var formClosedCount = 0;
            using (var dialog = new DownloadHistoryDialog(controller, preventActivationOnShow: true))
            {
                dialog.FormClosed += (sender, args) => formClosedCount++;
                dialog.Show();
                context.PumpEvents();

                var closeButton = ControlLookup.RequireControl<Button>(
                    dialog,
                    UiAutomationIds.DownloadHistory.CloseButton);
                closeButton.PerformClick();
                context.PumpEvents();

                TestAssert.False(dialog.Visible, "닫기 버튼이 다운로드 보기 창을 닫지 않았습니다.");
                TestAssert.Equal(1, formClosedCount, "FormClosed가 정확히 한 번 발생해야 합니다.");
            }
            TestAssert.Equal(0, controller.SubscriberCount, "닫힌 창이 컨트롤러 이벤트를 구독하고 있습니다.");
        }

        internal static void DownloadHistoryCancelButton(TestContext context)
        {
            var controller = new RecordingDownloadHistoryController();
            var formClosedCount = 0;
            using (var dialog = new DownloadHistoryDialog(controller, preventActivationOnShow: true))
            {
                dialog.FormClosed += (sender, args) => formClosedCount++;
                dialog.Show();
                context.PumpEvents();

                var cancelButton = dialog.CancelButton as Button;
                TestAssert.True(cancelButton != null, "다운로드 보기 CancelButton이 지정되지 않았습니다.");
                cancelButton.PerformClick();
                context.PumpEvents();

                TestAssert.False(dialog.Visible, "CancelButton 경로가 모델리스 창을 닫지 않았습니다.");
                TestAssert.Equal(1, formClosedCount, "CancelButton 경로의 FormClosed 횟수가 잘못되었습니다.");
            }
            TestAssert.Equal(0, controller.SubscriberCount, "CancelButton 닫기 후 이벤트 구독이 남았습니다.");
        }

        internal static void PageFindCommand(TestContext context)
        {
            var controller = new RecordingPageFindController();
            var initialCriteria = new PageFindCriteria { Term = "Windows XP" };
            using (var dialog = new PageFindDialog(
                controller,
                initialCriteria,
                preventActivationOnShow: true))
            {
                dialog.Show();
                context.PumpEvents();

                var findButton = ControlLookup.RequireControl<Button>(
                    dialog,
                    UiAutomationIds.PageFind.FindNextButton);
                findButton.PerformClick();
                context.PumpEvents();

                TestAssert.Equal(1, controller.FindCount, "찾기 컨트롤러가 정확히 한 번 호출되어야 합니다.");
                TestAssert.Equal("Windows XP", controller.LastCriteria.Term, "검색어가 컨트롤러에 전달되지 않았습니다.");
                TestAssert.False(controller.LastCriteria.SearchUp, "기본 찾기 방향은 아래여야 합니다.");
                TestAssert.False(controller.LastCriteria.MatchCase, "대소문자 구분 기본값이 잘못되었습니다.");
                TestAssert.False(controller.LastCriteria.MatchWholeWord, "단어 단위 일치 기본값이 잘못되었습니다.");
            }
            TestAssert.Equal(0, controller.SubscriberCount, "찾기 창 폐기 후 이벤트 구독이 남았습니다.");
        }

        internal static void DeleteBrowsingDataCommand(TestContext context)
        {
            var callCount = 0;
            var receivedSelection = BrowsingDataSelection.None;
            using (var dialog = new DeleteBrowsingDataDialog(
                selection =>
                {
                    callCount++;
                    receivedSelection = selection;
                    return Task.CompletedTask;
                },
                profileAvailable: true,
                sitePermissionsAvailable: true,
                preventActivationOnShow: true))
            {
                dialog.Show();
                context.PumpEvents();
                TestAssert.Equal(
                    BrowsingDataSelection.SafeDefaults,
                    dialog.Selection,
                    "검색 기록 삭제 기본 선택이 안전 기본값과 다릅니다.");

                foreach (var selection in BrowsingDataOptions)
                {
                    ControlLookup.RequireControl<CheckBox>(
                        dialog,
                        UiAutomationIds.BrowsingData.Option(selection)).Checked = false;
                }
                var deleteButton = ControlLookup.RequireControl<Button>(
                    dialog,
                    UiAutomationIds.BrowsingData.DeleteButton);
                TestAssert.False(deleteButton.Enabled, "선택 항목이 없을 때 삭제 버튼이 활성화되었습니다.");

                ControlLookup.RequireControl<CheckBox>(
                    dialog,
                    UiAutomationIds.BrowsingData.Option(BrowsingDataSelection.History)).Checked = true;
                TestAssert.True(deleteButton.Enabled, "삭제 항목 선택 후 삭제 버튼이 활성화되지 않았습니다.");
                deleteButton.PerformClick();
                context.PumpEvents();

                TestAssert.Equal(1, callCount, "삭제 콜백이 정확히 한 번 호출되어야 합니다.");
                TestAssert.Equal(
                    BrowsingDataSelection.History,
                    receivedSelection,
                    "선택한 삭제 항목이 콜백에 정확히 전달되지 않았습니다.");
                TestAssert.False(dialog.Visible, "삭제 성공 후 검색 기록 삭제 창이 닫히지 않았습니다.");
            }
        }

        private sealed class RecordingDownloadHistoryController : IDownloadHistoryController
        {
            private EventHandler _changed;

            public event EventHandler Changed
            {
                add => _changed += value;
                remove => _changed -= value;
            }

            internal int SubscriberCount => _changed?.GetInvocationList().Length ?? 0;

            public IReadOnlyList<DownloadRecord> Items => Array.Empty<DownloadRecord>();

            public bool Remove(string id)
            {
                return false;
            }

            public void Clear()
            {
            }
        }

        private sealed class RecordingPageFindController : IPageFindController
        {
            private EventHandler _stateChanged;

            public event EventHandler StateChanged
            {
                add => _stateChanged += value;
                remove => _stateChanged -= value;
            }

            internal int SubscriberCount => _stateChanged?.GetInvocationList().Length ?? 0;

            internal int FindCount { get; private set; }

            internal PageFindCriteria LastCriteria { get; private set; }

            public int ActiveMatchIndex => 1;

            public int MatchCount => 1;

            public PageFindCriteria CurrentCriteria => LastCriteria?.Clone();

            public Task FindAsync(PageFindCriteria criteria)
            {
                FindCount++;
                LastCriteria = criteria?.Clone();
                _stateChanged?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            }

            public Task RepeatAsync(bool previous)
            {
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
