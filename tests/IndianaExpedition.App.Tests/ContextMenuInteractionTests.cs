using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IndianaExpedition.ContextMenus;

namespace IndianaExpedition.App.Tests
{
    internal static class ContextMenuInteractionTests
    {
        private static readonly PageContextMenuCommand[] BoundCommands =
        {
            PageContextMenuCommand.Back,
            PageContextMenuCommand.Forward,
            PageContextMenuCommand.Refresh,
            PageContextMenuCommand.OpenLinkNewWindow,
            PageContextMenuCommand.CopyLink,
            PageContextMenuCommand.CopySelection,
            PageContextMenuCommand.SelectAll
        };

        internal static void CommandMapping(TestContext context)
        {
            var executed = new List<PageContextMenuCommand>();
            var completeDefinition = PageContextMenuFactory.Create(
                new PageContextMenuModel("https://example.com/", "selected"),
                CreateCommandMap(command => executed.Add(command)));
            try
            {
                completeDefinition.CommandInvoked += (sender, args) => args.Execute();
                foreach (var command in BoundCommands)
                {
                    executed.Clear();
                    var item = completeDefinition.GetItem(command);
                    TestAssert.True(item.Enabled, "활성 컨텍스트의 명령이 비활성화되었습니다: " + command);
                    item.PerformClick();
                    TestAssert.SequenceEqual(
                        new[] { command },
                        executed,
                        "우클릭 메뉴 항목의 명령 매핑이 잘못되었습니다: " + command);
                }

                TestAssert.True(
                    completeDefinition.Contains(PageContextMenuCommand.Properties),
                    "속성 자리 표시자 항목이 없습니다.");
                TestAssert.False(
                    completeDefinition.GetItem(PageContextMenuCommand.Properties).Enabled,
                    "속성 자리 표시자 항목은 비활성 상태여야 합니다.");
            }
            finally
            {
                completeDefinition.Menu.Dispose();
            }

            var pageDefinition = PageContextMenuFactory.Create(
                new PageContextMenuModel(linkUri: null, selectionText: null),
                CreateCommandMap(
                    command => { },
                    PageContextMenuCommand.Back,
                    PageContextMenuCommand.Forward));
            try
            {
                TestAssert.False(
                    pageDefinition.Contains(PageContextMenuCommand.OpenLinkNewWindow),
                    "링크가 없는 페이지에 링크 열기 명령이 표시되었습니다.");
                TestAssert.False(
                    pageDefinition.Contains(PageContextMenuCommand.CopyLink),
                    "링크가 없는 페이지에 바로 가기 복사 명령이 표시되었습니다.");
                TestAssert.False(
                    pageDefinition.GetItem(PageContextMenuCommand.CopySelection).Enabled,
                    "선택 영역이 없는데 복사 명령이 활성화되었습니다.");
                TestAssert.False(
                    pageDefinition.GetItem(PageContextMenuCommand.Back).Enabled,
                    "이동 기록이 없는데 뒤로 명령이 활성화되었습니다.");
                TestAssert.False(
                    pageDefinition.GetItem(PageContextMenuCommand.Forward).Enabled,
                    "이동 기록이 없는데 앞으로 명령이 활성화되었습니다.");
            }
            finally
            {
                pageDefinition.Menu.Dispose();
            }
        }

        internal static void DeferralBeforeCommand(TestContext context)
        {
            foreach (var selectedCommand in BoundCommands)
            {
                var events = new List<string>();
                var definition = PageContextMenuFactory.Create(
                    new PageContextMenuModel("https://example.com/", "selected"),
                    CreateCommandMap(command => events.Add("command:" + command)));
                var deferral = new RecordingDeferral(events);
                var dispatcher = new RecordingDispatcher(events, executeAction: true);
                using (var owner = CreateHandleOwner())
                using (var session = new WebViewContextMenuSession(
                    definition,
                    deferral,
                    dispatcher,
                    owner))
                {
                    definition.GetItem(selectedCommand).PerformClick();
                    session.Close();

                    TestAssert.SequenceEqual(
                        new[]
                        {
                            "complete",
                            "post",
                            "command:" + selectedCommand
                        },
                        events,
                        "deferral 완료, 예약, 명령 실행 순서가 잘못되었습니다: " + selectedCommand);
                    TestAssert.Equal(1, deferral.CompleteCount, "deferral은 정확히 한 번 완료되어야 합니다.");
                    TestAssert.Equal(1, dispatcher.PostCount, "명령은 정확히 한 번 예약되어야 합니다.");
                }
            }
        }

        internal static void CancelAndDuplicateDispose(TestContext context)
        {
            var events = new List<string>();
            var definition = PageContextMenuFactory.Create(
                new PageContextMenuModel("https://example.com/", "selected"),
                CreateCommandMap(command => events.Add("command:" + command)));
            var deferral = new RecordingDeferral(events);
            var dispatcher = new RecordingDispatcher(events, executeAction: true);
            using (var owner = CreateHandleOwner())
            {
                var session = new WebViewContextMenuSession(definition, deferral, dispatcher, owner);
                session.Dispose();
                session.Dispose();

                TestAssert.SequenceEqual(new[] { "complete" }, events, "취소 시 명령이 실행 또는 예약되었습니다.");
                TestAssert.Equal(1, deferral.CompleteCount, "중복 Dispose가 deferral을 다시 완료했습니다.");
                TestAssert.Equal(0, dispatcher.PostCount, "취소된 메뉴가 명령을 예약했습니다.");
            }
        }

        internal static void DisposedOwnerDropsCommand(TestContext context)
        {
            var events = new List<string>();
            var definition = PageContextMenuFactory.Create(
                new PageContextMenuModel("https://example.com/", "selected"),
                CreateCommandMap(command => events.Add("command:" + command)));
            var deferral = new RecordingDeferral(events);
            var dispatcher = new RecordingDispatcher(events, executeAction: true);
            var owner = CreateHandleOwner();
            var session = new WebViewContextMenuSession(definition, deferral, dispatcher, owner);
            owner.Dispose();

            definition.GetItem(PageContextMenuCommand.Refresh).PerformClick();
            session.Close();
            session.Dispose();

            TestAssert.SequenceEqual(new[] { "complete" }, events, "폐기된 소유자의 명령이 실행 또는 예약되었습니다.");
            TestAssert.Equal(1, deferral.CompleteCount, "폐기된 소유자에서도 deferral을 한 번 완료해야 합니다.");
            TestAssert.Equal(0, dispatcher.PostCount, "폐기된 소유자에 명령을 예약했습니다.");
        }

        internal static void DisabledItemDoesNotExecute(TestContext context)
        {
            var events = new List<string>();
            var definition = PageContextMenuFactory.Create(
                new PageContextMenuModel(linkUri: null, selectionText: null),
                CreateCommandMap(
                    command => events.Add("command:" + command),
                    PageContextMenuCommand.Back));
            var deferral = new RecordingDeferral(events);
            var dispatcher = new RecordingDispatcher(events, executeAction: true);
            using (var owner = CreateHandleOwner())
            using (var session = new WebViewContextMenuSession(
                definition,
                deferral,
                dispatcher,
                owner))
            {
                var disabledCommands = new[]
                {
                    PageContextMenuCommand.Back,
                    PageContextMenuCommand.CopySelection,
                    PageContextMenuCommand.Properties
                };
                foreach (var disabledCommand in disabledCommands)
                {
                    var disabledItem = definition.GetItem(disabledCommand);
                    TestAssert.False(
                        disabledItem.Enabled,
                        "테스트 대상 항목이 비활성 상태가 아닙니다: " + disabledCommand);
                    disabledItem.PerformClick();
                }
                session.Close();

                TestAssert.SequenceEqual(new[] { "complete" }, events, "비활성 항목의 명령이 실행 또는 예약되었습니다.");
                TestAssert.Equal(0, dispatcher.PostCount, "비활성 항목이 명령을 예약했습니다.");
            }
        }

        private static PageContextMenuCommandMap CreateCommandMap(
            Action<PageContextMenuCommand> execute,
            params PageContextMenuCommand[] disabledCommands)
        {
            var map = new PageContextMenuCommandMap();
            foreach (var command in BoundCommands)
            {
                var capturedCommand = command;
                var enabled = Array.IndexOf(disabledCommands, capturedCommand) < 0;
                map.Add(capturedCommand, () => execute(capturedCommand), enabled);
            }
            return map;
        }

        private static Control CreateHandleOwner()
        {
            var owner = new Control();
            var handle = owner.Handle;
            return owner;
        }

        private sealed class RecordingDeferral : IContextMenuDeferral
        {
            private readonly ICollection<string> _events;

            internal RecordingDeferral(ICollection<string> events)
            {
                _events = events;
            }

            internal int CompleteCount { get; private set; }

            public void Complete()
            {
                CompleteCount++;
                _events.Add("complete");
            }
        }

        private sealed class RecordingDispatcher : IUiCommandDispatcher
        {
            private readonly ICollection<string> _events;
            private readonly bool _executeAction;

            internal RecordingDispatcher(ICollection<string> events, bool executeAction)
            {
                _events = events;
                _executeAction = executeAction;
            }

            internal int PostCount { get; private set; }

            public bool TryPost(Control owner, Action action)
            {
                PostCount++;
                _events.Add("post");
                if (_executeAction)
                {
                    action();
                }
                return true;
            }
        }
    }
}
