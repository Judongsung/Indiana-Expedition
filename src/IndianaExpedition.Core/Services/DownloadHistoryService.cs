using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Services
{
    public sealed class DownloadHistoryService
    {
        private readonly object _gate = new object();
        private readonly AtomicJsonFileStore<DownloadHistoryDocument> _store;
        private readonly int _maximumEntries;
        private DownloadHistoryDocument _document;

        public DownloadHistoryService(
            string path,
            int maximumEntries = DownloadHistoryPolicy.MaximumEntries)
        {
            if (maximumEntries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumEntries));
            }

            _maximumEntries = maximumEntries;
            _store = new AtomicJsonFileStore<DownloadHistoryDocument>(
                path,
                DownloadHistoryDocument.CreateDefault);
            _document = Normalize(_store.Load());
        }

        public event EventHandler Changed;

        public IReadOnlyList<DownloadRecord> Items
        {
            get
            {
                lock (_gate)
                {
                    return _document.Items.Select(item => item.Clone()).ToList();
                }
            }
        }

        public DownloadRecord Add(DownloadRecord record)
        {
            var normalized = NormalizeRecord(record);
            lock (_gate)
            {
                _document.Items.RemoveAll(item =>
                    string.Equals(item.Id, normalized.Id, StringComparison.Ordinal));
                _document.Items.Insert(0, normalized);
                _document.Items = _document.Items
                    .OrderByDescending(item => item.FinishedAtUtc)
                    .Take(_maximumEntries)
                    .ToList();
                _store.Save(_document);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return normalized.Clone();
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            bool removed;
            lock (_gate)
            {
                removed = _document.Items.RemoveAll(item =>
                    string.Equals(item.Id, id, StringComparison.Ordinal)) > 0;
                if (removed)
                {
                    _store.Save(_document);
                }
            }

            if (removed)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
            return removed;
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

        private DownloadHistoryDocument Normalize(DownloadHistoryDocument document)
        {
            var result = document ?? DownloadHistoryDocument.CreateDefault();
            result.SchemaVersion = BrowserDefaults.DataSchemaVersion;
            result.Items = (result.Items ?? new List<DownloadRecord>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.FilePath))
                .Select(NormalizeRecord)
                .OrderByDescending(item => item.FinishedAtUtc)
                .Take(_maximumEntries)
                .ToList();
            return result;
        }

        private static DownloadRecord NormalizeRecord(DownloadRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }
            if (string.IsNullOrWhiteSpace(record.FilePath))
            {
                throw new ArgumentException(CoreMessages.DownloadFilePathRequired, nameof(record));
            }

            var filePath = Path.GetFullPath(record.FilePath);
            var finishedAtUtc = NormalizeUtc(
                record.FinishedAtUtc == default(DateTime)
                    ? DateTime.UtcNow
                    : record.FinishedAtUtc);
            var startedAtUtc = NormalizeUtc(
                record.StartedAtUtc == default(DateTime)
                    ? finishedAtUtc
                    : record.StartedAtUtc);

            return new DownloadRecord
            {
                Id = string.IsNullOrWhiteSpace(record.Id)
                    ? Guid.NewGuid().ToString(StorageConstants.CompactIdentifierFormat)
                    : record.Id.Trim(),
                FileName = string.IsNullOrWhiteSpace(record.FileName)
                    ? Path.GetFileName(filePath)
                    : record.FileName.Trim(),
                FilePath = filePath,
                StartedAtUtc = startedAtUtc,
                FinishedAtUtc = finishedAtUtc,
                BytesReceived = Math.Max(0L, record.BytesReceived),
                TotalBytes = record.TotalBytes > 0L ? record.TotalBytes : null,
                State = Enum.IsDefined(typeof(DownloadRecordState), record.State)
                    ? record.State
                    : DownloadRecordState.Failed
            };
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }
    }
}
