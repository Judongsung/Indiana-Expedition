using System;
using System.Collections.Generic;
using System.Linq;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Services
{
    public sealed class HistoryService
    {
        private readonly object _gate = new object();
        private readonly AtomicJsonFileStore<HistoryDocument> _store;
        private readonly int _retentionDays;
        private readonly int _maximumEntries;
        private HistoryDocument _document;

        public HistoryService(
            string path,
            int retentionDays = HistoryPolicy.RetentionDays,
            int maximumEntries = HistoryPolicy.MaximumEntries)
        {
            if (retentionDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retentionDays));
            }

            if (maximumEntries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumEntries));
            }

            _retentionDays = retentionDays;
            _maximumEntries = maximumEntries;
            _store = new AtomicJsonFileStore<HistoryDocument>(path, HistoryDocument.CreateDefault);
            _document = Normalize(_store.Load());
            Prune(DateTime.UtcNow);
        }

        public event EventHandler Changed;

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

            lock (_gate)
            {
                var normalizedTitle = string.IsNullOrWhiteSpace(title) ? url : title.Trim();
                var first = _document.Items.FirstOrDefault();

                if (first != null && string.Equals(first.Url, url, StringComparison.OrdinalIgnoreCase))
                {
                    first.Title = normalizedTitle;
                    first.VisitedAtUtc = timestamp;
                }
                else
                {
                    _document.Items.Insert(0, new HistoryEntry
                    {
                        Url = url,
                        Title = normalizedTitle,
                        VisitedAtUtc = timestamp
                    });
                }

                PruneLocked(timestamp);
                _store.Save(_document);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void Clear()
        {
            lock (_gate)
            {
                _document.Items.Clear();
                _store.Save(_document);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Prune(DateTime utcNow)
        {
            lock (_gate)
            {
                if (PruneLocked(utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime()))
                {
                    _store.Save(_document);
                }
            }
        }

        private bool PruneLocked(DateTime utcNow)
        {
            var cutoff = utcNow.AddDays(-_retentionDays);
            var normalized = _document.Items
                .Where(item => item != null && item.VisitedAtUtc >= cutoff)
                .OrderByDescending(item => item.VisitedAtUtc)
                .Take(_maximumEntries)
                .ToList();

            var changed = normalized.Count != _document.Items.Count ||
                          !normalized.SequenceEqual(_document.Items);
            _document.Items = normalized;
            return changed;
        }

        private static HistoryDocument Normalize(HistoryDocument document)
        {
            var result = document ?? HistoryDocument.CreateDefault();
            result.SchemaVersion = BrowserDefaults.DataSchemaVersion;
            result.Items = result.Items ?? new List<HistoryEntry>();
            return result;
        }
    }
}
