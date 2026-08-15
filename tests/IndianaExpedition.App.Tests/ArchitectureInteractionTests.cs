using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndianaExpedition.Browser;
using IndianaExpedition.Commands;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Dialogs;
using IndianaExpedition.Favorites;

namespace IndianaExpedition.App.Tests
{
    internal static class ArchitectureInteractionTests
    {
        private const int SoakWarmupCycles = 10;
        private const int SoakMeasurementCycles = 50;
        private const int MaximumResourceGrowth = 12;
        private const int GdiResourceType = 0;
        private const int UserResourceType = 1;

        internal static void RecentAddressLimit(TestContext context)
        {
            var history = new RecentAddressHistory();
            for (var index = 0; index < 105; index++)
            {
                history.Remember("https://site" + index + ".example/");
            }
            history.Remember("HTTPS://SITE100.EXAMPLE/");
            TestAssert.Equal(100, history.Items.Count, "최근 주소는 100개를 넘으면 안 됩니다.");
            TestAssert.Equal(
                "HTTPS://SITE100.EXAMPLE/",
                history.Items[0],
                "중복 주소를 최신 표기로 맨 앞으로 옮겨야 합니다.");
        }

        internal static void CommandCatalogRouting(TestContext context)
        {
            var executions = 0;
            var definition = new BrowserCommandDefinition(
                BrowserCommandId.Refresh,
                () => "refresh",
                new[] { Keys.F5, Keys.Control | Keys.R },
                () => true,
                () => false,
                () =>
                {
                    executions++;
                    return Task.CompletedTask;
                });
            var executor = new RecordingUiCommandExecutor();
            var router = new BrowserCommandRouter(
                new BrowserCommandCatalog(new[] { definition }),
                executor);
            TestAssert.True(router.TryExecuteShortcut(Keys.F5), "catalog가 첫 단축키를 찾아야 합니다.");
            TestAssert.True(router.TryExecuteShortcut(Keys.Control | Keys.R), "catalog가 보조 단축키를 찾아야 합니다.");
            TestAssert.Equal(2, executions, "두 입력 경로가 같은 명령을 실행해야 합니다.");
            TestAssert.Equal(2, executor.ExecutionCount, "명령 executor를 우회하면 안 됩니다.");
        }

        internal static void HistoryIncrementalPresenter(TestContext context)
        {
            var items = new List<HistoryEntry>();
            using (var tree = new TreeView())
            {
                var presenter = new HistorySidebarPresenter(tree, () => items);
                presenter.Rebuild();
                var entry = new HistoryEntry
                {
                    Url = "https://incremental.example/",
                    Title = "Incremental",
                    VisitedAtUtc = DateTime.UtcNow
                };
                items.Add(entry);
                presenter.Apply(new HistoryChangedEventArgs(HistoryChangeKind.Upsert, entry, 0));
                TestAssert.Equal(1, tree.Nodes.Count, "증분 변경이 날짜 그룹을 추가해야 합니다.");
                TestAssert.Equal(1, tree.Nodes[0].Nodes.Count, "증분 변경이 항목 하나만 추가해야 합니다.");
                var repeatedEntry = new HistoryEntry
                {
                    Url = entry.Url,
                    Title = "Incremental revisit",
                    VisitedAtUtc = entry.VisitedAtUtc.AddMinutes(1)
                };
                items.Insert(0, repeatedEntry);
                presenter.Apply(new HistoryChangedEventArgs(HistoryChangeKind.Upsert, repeatedEntry, 0));
                TestAssert.Equal(
                    2,
                    tree.Nodes[0].Nodes.Count,
                    "서비스가 보존한 이전 방문을 증분 presenter가 임의로 제거하면 안 됩니다.");
                presenter.Apply(new HistoryChangedEventArgs(HistoryChangeKind.Reset, null, -1));
                TestAssert.Equal(2, tree.Nodes[0].Nodes.Count, "대량 변경은 전체 재구성해야 합니다.");
            }
        }

        internal static void FavoritesPresenter(TestContext context)
        {
            var folder = FavoriteNode.CreateFolder("자료");
            folder.Children.Add(FavoriteNode.CreateLink("Microsoft", "https://www.microsoft.com/"));
            var items = new List<FavoriteNode> { folder };
            using (var tree = new TreeView())
            {
                var presenter = new FavoritesSidebarPresenter(tree, () => items);
                presenter.Rebuild();
                TestAssert.Equal(1, tree.Nodes.Count, "즐겨찾기 presenter가 루트 폴더를 투영해야 합니다.");
                TestAssert.Equal(1, tree.Nodes[0].Nodes.Count, "즐겨찾기 presenter가 자식 링크를 투영해야 합니다.");
                TestAssert.Equal(
                    "https://www.microsoft.com/",
                    ((FavoriteNode)tree.Nodes[0].Nodes[0].Tag).Url,
                    "사이드바 노드가 원본 즐겨찾기 모델을 보존해야 합니다.");
            }
        }

        internal static void GdiUserSoak(TestContext context)
        {
            RunCycles(context, SoakWarmupCycles);
            RunCycles(context, SoakMeasurementCycles);
            ForceCollection();
            var first = ReadGuiResources();
            RunCycles(context, SoakMeasurementCycles);
            ForceCollection();
            var second = ReadGuiResources();
            TestAssert.True(
                second.Gdi - first.Gdi <= MaximumResourceGrowth,
                "두 번째 구간 GDI 핸들 증가가 허용치를 넘었습니다.");
            TestAssert.True(
                second.User - first.User <= MaximumResourceGrowth,
                "두 번째 구간 USER 핸들 증가가 허용치를 넘었습니다.");
        }

        private static void RunCycles(TestContext context, int count)
        {
            for (var index = 0; index < count; index++)
            {
                using (var host = new BrowserTestHost(context, VisualTestState.Main))
                using (var about = new AboutDialog(preventActivationOnShow: true))
                using (var organize = new OrganizeFavoritesDialog(host.Services.Favorites))
                {
                    context.PumpEvents();
                }
            }
        }

        private static void ForceCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static GuiResourceSnapshot ReadGuiResources()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return new GuiResourceSnapshot(
                    GetGuiResources(process.Handle, GdiResourceType),
                    GetGuiResources(process.Handle, UserResourceType));
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetGuiResources(IntPtr process, int flags);

        private sealed class RecordingUiCommandExecutor : IUiCommandExecutor
        {
            internal int ExecutionCount { get; private set; }

            public void Execute(Func<Task> command)
            {
                ExecutionCount++;
                command().GetAwaiter().GetResult();
            }
        }

        private sealed class GuiResourceSnapshot
        {
            internal GuiResourceSnapshot(int gdi, int user)
            {
                Gdi = gdi;
                User = user;
            }
            internal int Gdi { get; }
            internal int User { get; }
        }
    }
}
