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
                foreach (var command in BoundCommands)
                {
                    executed.Clear();
                    var item = completeDefinition.GetItem(command);
                    TestAssert.True(item.Enabled, "활성 컨텍스트의 명령이 비활성화되었습니다: " + command);
                    TestAssert.True(
                        completeDefinition.TryGetCommand(item, out var execute),
                        "우클릭 메뉴 항목에 명령이 연결되지 않았습니다: " + command);
                    execute();
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
                TestAssert.False(
                    completeDefinition.TryGetCommand(
                        completeDefinition.GetItem(PageContextMenuCommand.Properties),
                        out _),
                    "속성 자리 표시자 항목에 실행 명령이 연결되었습니다.");
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
                using (var menu = new LifecycleContextMenuStrip(events))
                {
                    var definition = CreateLifecycleDefinition(
                        menu,
                        selectedCommand,
                        enabled: true,
                        () => events.Add("command:" + selectedCommand));
                    var deferral = new RecordingDeferral(events);
                    var dispatcher = new RecordingDispatcher(events);
                    using (var owner = CreateHandleOwner())
                    using (var session = new WebViewContextMenuSession(
                        definition,
                        deferral,
                        dispatcher,
                        owner))
                    {
                        menu.SelectItem(definition.GetItem(selectedCommand));

                        TestAssert.SequenceEqual(
                            new[]
                            {
                                "item-clicked",
                                "closed:start",
                                "complete",
                                "post",
                                "closed:end"
                            },
                            events,
                            "실제 메뉴 선택 이벤트 순서가 잘못되었습니다: " + selectedCommand);
                        TestAssert.False(menu.DisposeObserved, "Closed 처리 중 메뉴가 폐기되었습니다.");
                        TestAssert.Equal(0, dispatcher.ExecutedCount, "명령이 비동기 예약 전에 실행되었습니다.");

                        dispatcher.ExecutePostedActions();

                        TestAssert.SequenceEqual(
                            new[]
                            {
                                "item-clicked",
                                "closed:start",
                                "complete",
                                "post",
                                "closed:end",
                                "dispatch",
                                "dispose",
                                "command:" + selectedCommand
                            },
                            events,
                            "deferral 완료, 예약, 명령 실행 순서가 잘못되었습니다: " + selectedCommand);
                        TestAssert.Equal(1, deferral.CompleteCount, "deferral은 정확히 한 번 완료되어야 합니다.");
                        TestAssert.Equal(1, dispatcher.PostCount, "명령은 정확히 한 번 예약되어야 합니다.");
                        TestAssert.Equal(1, dispatcher.ExecutedCount, "예약된 명령은 정확히 한 번 실행되어야 합니다.");
                    }
                }
            }
        }

        internal static void CancelAndDuplicateDispose(TestContext context)
        {
            var events = new List<string>();
            using (var menu = new LifecycleContextMenuStrip(events))
            {
                var definition = CreateLifecycleDefinition(
                    menu,
                    PageContextMenuCommand.Refresh,
                    enabled: true,
                    () => events.Add("command"));
                var deferral = new RecordingDeferral(events);
                var dispatcher = new RecordingDispatcher(events);
                using (var owner = CreateHandleOwner())
                {
                    var session = new WebViewContextMenuSession(definition, deferral, dispatcher, owner);
                    menu.CloseWithoutSelection();
                    session.Dispose();
                    session.Dispose();

                    TestAssert.SequenceEqual(
                        new[] { "closed:start", "complete", "post", "closed:end" },
                        events,
                        "취소 시 deferral 및 정리 예약 순서가 잘못되었습니다.");
                    TestAssert.False(menu.DisposeObserved, "Closed 처리 중 취소된 메뉴가 폐기되었습니다.");

                    dispatcher.ExecutePostedActions();

                    TestAssert.SequenceEqual(
                        new[]
                        {
                            "closed:start",
                            "complete",
                            "post",
                            "closed:end",
                            "dispatch",
                            "dispose"
                        },
                        events,
                        "취소된 메뉴가 명령을 실행했거나 정리 순서가 잘못되었습니다.");
                    TestAssert.Equal(1, deferral.CompleteCount, "중복 Dispose가 deferral을 다시 완료했습니다.");
                    TestAssert.Equal(1, dispatcher.PostCount, "취소된 메뉴 정리가 정확히 한 번 예약되어야 합니다.");
                    TestAssert.Equal(1, dispatcher.ExecutedCount, "취소된 메뉴 정리가 정확히 한 번 실행되어야 합니다.");
                }
            }
        }

        internal static void DisposedOwnerDropsCommand(TestContext context)
        {
            var events = new List<string>();
            using (var menu = new LifecycleContextMenuStrip(events))
            {
                var definition = CreateLifecycleDefinition(
                    menu,
                    PageContextMenuCommand.Refresh,
                    enabled: true,
                    () => events.Add("command"));
                var deferral = new RecordingDeferral(events);
                var dispatcher = new RecordingDispatcher(events);
                var owner = CreateHandleOwner();
                var session = new WebViewContextMenuSession(definition, deferral, dispatcher, owner);
                owner.Dispose();

                menu.SelectItem(definition.GetItem(PageContextMenuCommand.Refresh));
                session.Dispose();

                TestAssert.SequenceEqual(
                    new[] { "item-clicked", "closed:start", "complete", "dispose", "closed:end" },
                    events,
                    "폐기된 소유자의 명령이 실행 또는 예약되었습니다.");
                TestAssert.Equal(1, deferral.CompleteCount, "폐기된 소유자에서도 deferral을 한 번 완료해야 합니다.");
                TestAssert.Equal(0, dispatcher.PostCount, "폐기된 소유자에 명령을 예약했습니다.");
            }
        }

        internal static void DisabledItemDoesNotExecute(TestContext context)
        {
            var disabledCommands = new[]
            {
                PageContextMenuCommand.Back,
                PageContextMenuCommand.CopySelection,
                PageContextMenuCommand.Properties
            };
            foreach (var disabledCommand in disabledCommands)
            {
                var events = new List<string>();
                using (var menu = new LifecycleContextMenuStrip(events))
                {
                    var definition = CreateLifecycleDefinition(
                        menu,
                        disabledCommand,
                        enabled: false,
                        () => events.Add("command"));
                    var deferral = new RecordingDeferral(events);
                    var dispatcher = new RecordingDispatcher(events);
                    using (var owner = CreateHandleOwner())
                    using (var session = new WebViewContextMenuSession(
                        definition,
                        deferral,
                        dispatcher,
                        owner))
                    {
                        var disabledItem = definition.GetItem(disabledCommand);
                        TestAssert.False(
                            disabledItem.Enabled,
                            "테스트 대상 항목이 비활성 상태가 아닙니다: " + disabledCommand);
                        menu.SelectItem(disabledItem);
                        dispatcher.ExecutePostedActions();

                        TestAssert.False(events.Contains("command"), "비활성 항목이 명령을 실행했습니다.");
                        TestAssert.Equal(1, deferral.CompleteCount, "비활성 항목에서도 deferral을 완료해야 합니다.");
                        TestAssert.Equal(1, dispatcher.PostCount, "비활성 항목의 메뉴 정리가 예약되지 않았습니다.");
                    }
                }
            }
        }

        private static PageContextMenuDefinition CreateLifecycleDefinition(
            ContextMenuStrip menu,
            PageContextMenuCommand command,
            bool enabled,
            Action execute)
        {
            var definition = new PageContextMenuDefinition(menu);
            if (command == PageContextMenuCommand.Properties)
            {
                definition.AddDisabledItem(command, command.ToString());
            }
            else
            {
                definition.AddCommand(command, command.ToString(), execute, enabled);
            }
            return definition;
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
            private readonly Queue<Action> _postedActions = new Queue<Action>();

            internal RecordingDispatcher(ICollection<string> events)
            {
                _events = events;
            }

            internal int PostCount { get; private set; }

            internal int ExecutedCount { get; private set; }

            public bool TryPost(Control owner, Action action)
            {
                PostCount++;
                _events.Add("post");
                _postedActions.Enqueue(action);
                return true;
            }

            internal void ExecutePostedActions()
            {
                while (_postedActions.Count > 0)
                {
                    var action = _postedActions.Dequeue();
                    ExecutedCount++;
                    _events.Add("dispatch");
                    action();
                }
            }
        }

        private sealed class LifecycleContextMenuStrip : ContextMenuStrip
        {
            private readonly ICollection<string> _events;
            private bool _closedRaised;
            private bool _disposeRecorded;

            internal LifecycleContextMenuStrip(ICollection<string> events)
            {
                _events = events;
            }

            internal bool DisposeObserved => _disposeRecorded;

            internal void SelectItem(ToolStripItem item)
            {
                _events.Add("item-clicked");
                OnItemClicked(new ToolStripItemClickedEventArgs(item));
                RaiseClosed(ToolStripDropDownCloseReason.ItemClicked);
            }

            internal void CloseWithoutSelection()
            {
                RaiseClosed(ToolStripDropDownCloseReason.CloseCalled);
            }

            protected override void OnClosed(ToolStripDropDownClosedEventArgs args)
            {
                _closedRaised = true;
                _events.Add("closed:start");
                base.OnClosed(args);
                _events.Add("closed:end");
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_disposeRecorded)
                {
                    _disposeRecorded = true;
                    _events.Add("dispose");
                }
                base.Dispose(disposing);
            }

            private void RaiseClosed(ToolStripDropDownCloseReason reason)
            {
                if (!_closedRaised)
                {
                    OnClosed(new ToolStripDropDownClosedEventArgs(reason));
                }
            }
        }
    }
}
