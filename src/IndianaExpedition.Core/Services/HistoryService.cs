using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Services
{
    internal enum HistoryChangeKind
    {
        Upsert = 0,
        Reset = 1
    }

    internal sealed class HistoryChangedEventArgs : EventArgs
    {
        internal HistoryChangedEventArgs(
            HistoryChangeKind kind,
            HistoryEntry entry,
            int index,
            IEnumerable<HistoryEntry> removedEntries = null)
        {
            Kind = kind;
            Entry = entry?.Clone();
            Index = index;
            RemovedEntries = (removedEntries ?? Enumerable.Empty<HistoryEntry>())
                .Select(item => item.Clone())
                .ToList();
        }

        internal HistoryChangeKind Kind { get; }
        internal HistoryEntry Entry { get; }
        internal int Index { get; }
        internal IReadOnlyList<HistoryEntry> RemovedEntries { get; }
    }

    public sealed class HistoryService : IFlushablePersistence, IDisposable
    {
        private readonly object _gate = new object();
        private readonly IDeferredDocumentWriter<HistoryDocument> _writer;
        private readonly int _retentionDays;
        private readonly int _maximumEntries;
        private HistoryDocument _document;

        public HistoryService(
            string path,
            int retentionDays = HistoryPolicy.RetentionDays,
            int maximumEntries = HistoryPolicy.MaximumEntries)
            : this(
                new AtomicJsonFileStore<HistoryDocument>(path, HistoryDocument.CreateDefault),
                retentionDays,
                maximumEntries)
        {
        }

        internal HistoryService(
            IDocumentStore<HistoryDocument> store,
            int retentionDays = HistoryPolicy.RetentionDays,
            int maximumEntries = HistoryPolicy.MaximumEntries)
            : this(
                store,
                new DebouncedDocumentWriter<HistoryDocument>(store, document => document.DeepClone()),
                retentionDays,
                maximumEntries)
        {
        }

        internal HistoryService(
            IDocumentStore<HistoryDocument> store,
            IDeferredDocumentWriter<HistoryDocument> writer,
            int retentionDays,
            int maximumEntries)
        {
            if (retentionDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retentionDays));
            }
            if (maximumEntries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumEntries));
            }
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            _retentionDays = retentionDays;
            _maximumEntries = maximumEntries;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _writer.PersistenceWriteFailed += OnPersistenceWriteFailed;
            _document = Normalize(store.Load());
            if (PruneLocked(DateTime.UtcNow))
            {
                _writer.Schedule(_document.DeepClone());
            }
        }

        public event EventHandler Changed;
        internal event EventHandler<HistoryChangedEventArgs> DetailedChanged;
        internal event EventHandler<PersistenceWriteFailedEventArgs> PersistenceWriteFailed;

        event EventHandler<PersistenceWriteFailedEventArgs> IFlushablePersistence.PersistenceWriteFailed
        {
            add { PersistenceWriteFailed += value; }
            remove { PersistenceWriteFailed -= value; }
        }

        public IReadOnlyList<HistoryEntry> Items
        {
            get
            {
                lock (_gate)
                {
                    return _document.Items.Select(item => item.Clone()).ToList();
                }
            }
        }

        public bool RecordNavigation(string url, string title, DateTime visitedAtUtc)
        {
            if (!AddressResolver.IsHistoryEligible(url))
            {
                return false;
            }

            var timestamp = visitedAtUtc.Kind == DateTimeKind.Utc
                ? visitedAtUtc
                : visitedAtUtc.ToUniversalTime();
            HistoryEntry entry;
            int index;
            HistoryDocument snapshot;
            var removedEntries = new List<HistoryEntry>();
            lock (_gate)
            {
                entry = new HistoryEntry
                {
                    Url = url,
                    Title = string.IsNullOrWhiteSpace(title) ? url : title.Trim(),
                    VisitedAtUtc = timestamp
                };
                if (_document.Items.Count > 0 &&
                    string.Equals(_document.Items[0].Url, url, StringComparison.OrdinalIgnoreCase))
                {
                    removedEntries.Add(_document.Items[0].Clone());
                    _document.Items.RemoveAt(0);
                }
                index = FindInsertionIndex(_document.Items, timestamp);
                _document.Items.Insert(index, entry);
                PruneLocked(DateTime.UtcNow, removedEntries);
                snapshot = _document.DeepClone();
            }

            _writer.Schedule(snapshot);
            DetailedChanged?.Invoke(
                this,
                new HistoryChangedEventArgs(HistoryChangeKind.Upsert, entry, index, removedEntries));
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void Clear()
        {
            lock (_gate)
            {
                var candidate = HistoryDocument.CreateDefault();
                var saved = _writer.SaveImmediatelyAsync(
                        candidate,
                        TimeSpan.FromMilliseconds(PersistencePolicy.ShutdownFlushTimeoutMilliseconds))
                    .GetAwaiter()
                    .GetResult();
                if (!saved)
                {
                    throw new TimeoutException();
                }
                _document = candidate;
            }
            DetailedChanged?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Reset, null, -1));
            Changed?.Invoke(this, EventArgs.Empty);
        }

        internal Task ClearAsync()
        {
            return Task.Run((Action)Clear);
        }

        public void Prune(DateTime utcNow)
        {
            HistoryDocument snapshot = null;
            lock (_gate)
            {
                if (PruneLocked(utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime()))
                {
                    snapshot = _document.DeepClone();
                }
            }
            if (snapshot == null)
            {
                return;
            }
            _writer.Schedule(snapshot);
            DetailedChanged?.Invoke(this, new HistoryChangedEventArgs(HistoryChangeKind.Reset, null, -1));
            Changed?.Invoke(this, EventArgs.Empty);
        }

        Task<bool> IFlushablePersistence.FlushAsync(TimeSpan timeout)
        {
            return FlushAsync(timeout);
        }

        internal Task<bool> FlushAsync(TimeSpan timeout)
        {
            return _writer.FlushAsync(timeout);
        }

        public void Dispose()
        {
            _writer.PersistenceWriteFailed -= OnPersistenceWriteFailed;
            _writer.Dispose();
        }

        private bool PruneLocked(DateTime utcNow, ICollection<HistoryEntry> removedEntries = null)
        {
            var changed = false;
            var cutoff = utcNow.AddDays(-_retentionDays);
            while (_document.Items.Count > 0 &&
                   _document.Items[_document.Items.Count - 1].VisitedAtUtc < cutoff)
            {
                removedEntries?.Add(_document.Items[_document.Items.Count - 1].Clone());
                _document.Items.RemoveAt(_document.Items.Count - 1);
                changed = true;
            }
            if (_document.Items.Count > _maximumEntries)
            {
                if (removedEntries != null)
                {
                    for (var index = _maximumEntries; index < _document.Items.Count; index++)
                    {
                        removedEntries.Add(_document.Items[index].Clone());
                    }
                }
                _document.Items.RemoveRange(_maximumEntries, _document.Items.Count - _maximumEntries);
                changed = true;
            }
            return changed;
        }

        private static int FindInsertionIndex(IReadOnlyList<HistoryEntry> items, DateTime timestamp)
        {
            var low = 0;
            var high = items.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (items[middle].VisitedAtUtc >= timestamp)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }
            return low;
        }

        private static HistoryDocument Normalize(HistoryDocument document)
        {
            var result = document?.DeepClone() ?? HistoryDocument.CreateDefault();
            result.SchemaVersion = BrowserDefaults.DataSchemaVersion;
            result.Items = (result.Items ?? new List<HistoryEntry>())
                .Where(item => item != null && AddressResolver.IsHistoryEligible(item.Url))
                .Select(item => item.Clone())
                .OrderByDescending(item => item.VisitedAtUtc)
                .ToList();
            return result;
        }

        private void OnPersistenceWriteFailed(object sender, PersistenceWriteFailedEventArgs args)
        {
            PersistenceWriteFailed?.Invoke(this, args);
        }
    }
}
