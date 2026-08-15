using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Persistence;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition.Core.Tests
{
    internal static class DeferredPersistenceTests
    {
        private const int FlushBehaviorTimeoutMilliseconds = 100;
        private const int FlushBehaviorMaximumElapsedMilliseconds = 1000;

        internal static void Run()
        {
            TestAssert.Equal(500, PersistencePolicy.DebounceMilliseconds, "debounce 정책이 달라졌습니다.");
            TestAssert.Equal(4, PersistencePolicy.MaximumSaveAttempts, "저장 시도 횟수가 달라졌습니다.");
            TestAssert.Equal(
                2000,
                PersistencePolicy.ShutdownFlushTimeoutMilliseconds,
                "종료 flush 제한 시간이 달라졌습니다.");

            var sessionStore = new MemoryDocumentStore<SessionState>(
                SessionState.CreateDefault(),
                value => value.Clone());
            using (var session = new SessionService(sessionStore))
            {
                session.Remember("https://one.example/");
                session.Remember("https://two.example/");
                session.Remember("https://three.example/");
                Thread.Sleep(PersistencePolicy.DebounceMilliseconds + 350);
                TestAssert.Equal(1, sessionStore.SaveCount, "연속 세션 변경을 한 번으로 합쳐야 합니다.");
                TestAssert.Equal("https://three.example/", sessionStore.LastSaved.LastActiveUrl, "최신 세션 스냅숏을 저장해야 합니다.");
                session.Remember("https://three.example/");
                Thread.Sleep(PersistencePolicy.DebounceMilliseconds + 100);
                TestAssert.Equal(1, sessionStore.SaveCount, "같은 URL을 다시 저장하면 안 됩니다.");
            }

            var coalescedHistoryStore = new MemoryDocumentStore<HistoryDocument>(
                HistoryDocument.CreateDefault(),
                value => value.DeepClone());
            using (var coalescedHistory = new HistoryService(coalescedHistoryStore, 30, 2000))
            {
                var now = DateTime.UtcNow;
                coalescedHistory.RecordNavigation("https://one.example/", "One", now);
                coalescedHistory.RecordNavigation("https://two.example/", "Two", now.AddSeconds(1));
                coalescedHistory.RecordNavigation("https://three.example/", "Three", now.AddSeconds(2));
                Thread.Sleep(PersistencePolicy.DebounceMilliseconds + 350);
                TestAssert.Equal(1, coalescedHistoryStore.SaveCount, "연속 방문 기록 변경을 한 번으로 합쳐야 합니다.");
                TestAssert.Equal(3, coalescedHistoryStore.LastSaved.Items.Count, "최신 방문 기록 스냅숏을 저장해야 합니다.");
            }

            var historyStore = new MemoryDocumentStore<HistoryDocument>(
                HistoryDocument.CreateDefault(),
                value => value.DeepClone());
            using (var history = new HistoryService(historyStore, 30, 2000))
            {
                var now = DateTime.UtcNow;
                for (var index = 0; index < 2000; index++)
                {
                    history.RecordNavigation(
                        "https://site" + index + ".example/",
                        "Site " + index,
                        now.AddSeconds(-index));
                }
                TestAssert.Equal(2000, history.Items.Count, "2,000개 보관 정책이 잘못되었습니다.");
                TestAssert.True(
                    history.Items[0].VisitedAtUtc >= history.Items[1999].VisitedAtUtc,
                    "기록이 최신순이어야 합니다.");
                history.Clear();
                Thread.Sleep(PersistencePolicy.DebounceMilliseconds + 150);
                TestAssert.Equal(0, historyStore.LastSaved.Items.Count, "이전 pending 스냅숏이 삭제 기록을 복구하면 안 됩니다.");
            }

            var failingStore = new MemoryDocumentStore<SessionState>(
                SessionState.CreateDefault(),
                value => value.Clone()) { FailWrites = true };
            var retryDelays = new List<int>();
            using (var writer = new DebouncedDocumentWriter<SessionState>(
                failingStore,
                value => value.Clone(),
                milliseconds =>
                {
                    retryDelays.Add(milliseconds);
                    return Task.CompletedTask;
                }))
            {
                var failures = 0;
                writer.PersistenceWriteFailed += (sender, args) => failures++;
                writer.Schedule(new SessionState { SchemaVersion = 3, LastActiveUrl = "https://retry.example/" });
                var flushed = writer.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
                TestAssert.True(!flushed, "모든 재시도가 실패하면 flush가 실패해야 합니다.");
                TestAssert.Equal(4, failingStore.SaveCount, "초기 저장과 세 번의 재시도만 수행해야 합니다.");
                TestAssert.Equal(1, failures, "실패 이벤트는 한 번만 발행해야 합니다.");
                TestAssert.True(
                    retryDelays.SequenceEqual(new[]
                    {
                        PersistencePolicy.FirstRetryDelayMilliseconds,
                        PersistencePolicy.SecondRetryDelayMilliseconds,
                        PersistencePolicy.ThirdRetryDelayMilliseconds
                    }),
                    "재시도 지연 순서가 정책과 일치해야 합니다.");
                failingStore.FailWrites = false;
                writer.Schedule(new SessionState { SchemaVersion = 3, LastActiveUrl = "https://recovered.example/" });
                TestAssert.True(
                    writer.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(),
                    "후속 변경에서 저장이 복구되어야 합니다.");
            }

            using (var blockingStore = new BlockingDocumentStore<SessionState>(
                SessionState.CreateDefault(),
                value => value.Clone()))
            using (var writer = new DebouncedDocumentWriter<SessionState>(
                blockingStore,
                value => value.Clone()))
            {
                writer.Schedule(new SessionState
                {
                    SchemaVersion = BrowserDefaults.DataSchemaVersion,
                    LastActiveUrl = "https://flush-timeout.example/"
                });
                var stopwatch = Stopwatch.StartNew();
                var flush = writer.FlushAsync(
                    TimeSpan.FromMilliseconds(FlushBehaviorTimeoutMilliseconds));
                TestAssert.True(
                    blockingStore.WaitUntilSaveStarts(TimeSpan.FromSeconds(1)),
                    "flush 저장 작업이 시작되어야 합니다.");
                TestAssert.True(!flush.GetAwaiter().GetResult(), "제한 시간을 넘긴 flush는 실패해야 합니다.");
                stopwatch.Stop();
                TestAssert.True(
                    stopwatch.ElapsedMilliseconds < FlushBehaviorMaximumElapsedMilliseconds,
                    "flush가 지정된 제한 시간을 크게 초과하면 안 됩니다.");
                blockingStore.ReleaseSave();
                TestAssert.True(
                    writer.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(),
                    "제한 시간 이후에도 보존된 dirty 스냅숏을 다시 저장할 수 있어야 합니다.");
            }
        }
    }
}
