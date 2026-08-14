using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IndianaExpedition.App.Tests
{
    internal static class Program
    {
        private static readonly IReadOnlyList<TestCase> TestCases = new[]
        {
            new TestCase("다운로드 보기 닫기 이벤트", DialogInteractionTests.DownloadHistoryCloseButton),
            new TestCase("다운로드 보기 CancelButton 이벤트", DialogInteractionTests.DownloadHistoryCancelButton),
            new TestCase("페이지 찾기 명령 전달", DialogInteractionTests.PageFindCommand),
            new TestCase("검색 기록 삭제 선택 전달", DialogInteractionTests.DeleteBrowsingDataCommand),
            new TestCase("즐겨찾기와 기록 사이드바 토글", BrowserInteractionTests.ExplorerSidebarToggle),
            new TestCase("상위 메뉴 재클릭 닫기", BrowserInteractionTests.TopLevelMenuToggle),
            new TestCase("팝업 정보 표시줄 닫기", BrowserInteractionTests.PopupInformationBarClose),
            new TestCase("팝업 사이트 영구 허용", BrowserInteractionTests.PopupOriginAllow),
            new TestCase("우클릭 메뉴 조건과 명령 매핑", ContextMenuInteractionTests.CommandMapping),
            new TestCase("우클릭 메뉴 deferral 순서", ContextMenuInteractionTests.DeferralBeforeCommand),
            new TestCase("우클릭 메뉴 취소와 중복 Dispose", ContextMenuInteractionTests.CancelAndDuplicateDispose),
            new TestCase("우클릭 메뉴 폐기 소유자 처리", ContextMenuInteractionTests.DisposedOwnerDropsCommand),
            new TestCase("우클릭 메뉴 비활성 항목 처리", ContextMenuInteractionTests.DisabledItemDoesNotExecute)
        };

        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var failures = new List<string>();
            using (var foregroundGuard = new ForegroundWindowGuard())
            {
                foreach (var testCase in TestCases)
                {
                    try
                    {
                        using (var context = new TestContext(foregroundGuard))
                        {
                            testCase.Execute(context);
                            context.PumpEvents();
                        }
                        foregroundGuard.ThrowIfViolated();
                        Console.WriteLine("PASS: " + testCase.Name);
                    }
                    catch (Exception exception)
                    {
                        failures.Add(testCase.Name + ": " + exception.Message);
                    }
                }
            }

            if (failures.Count == 0)
            {
                Console.WriteLine("PASS: IndianaExpedition.App 동작 테스트가 모두 통과했습니다.");
                return 0;
            }

            Console.Error.WriteLine("FAIL: " + failures.Count + "개 App 동작 테스트가 실패했습니다.");
            foreach (var failure in failures)
            {
                Console.Error.WriteLine(" - " + failure);
            }
            return 1;
        }

        private sealed class TestCase
        {
            internal TestCase(string name, Action<TestContext> execute)
            {
                Name = name;
                Execute = execute;
            }

            internal string Name { get; }

            internal Action<TestContext> Execute { get; }
        }
    }
}
