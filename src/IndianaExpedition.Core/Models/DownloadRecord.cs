using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Models
{
    public enum DownloadRecordState
    {
        Completed = 0,
        Failed = 1,
        Canceled = 2
    }

    [DataContract]
    public sealed class DownloadRecord
    {
        [DataMember(Order = 0)]
        public string Id { get; set; }

        [DataMember(Order = 1)]
        public string FileName { get; set; }

        [DataMember(Order = 2)]
        public string FilePath { get; set; }

        [DataMember(Order = 3)]
        public DateTime StartedAtUtc { get; set; }

        [DataMember(Order = 4)]
        public DateTime FinishedAtUtc { get; set; }

        [DataMember(Order = 5)]
        public long BytesReceived { get; set; }

        [DataMember(Order = 6)]
        public long? TotalBytes { get; set; }

        [DataMember(Order = 7)]
        public DownloadRecordState State { get; set; }

        public DownloadRecord Clone()
        {
            return new DownloadRecord
            {
                Id = Id,
                FileName = FileName,
                FilePath = FilePath,
                StartedAtUtc = StartedAtUtc,
                FinishedAtUtc = FinishedAtUtc,
                BytesReceived = BytesReceived,
                TotalBytes = TotalBytes,
                State = State
            };
        }
    }

    [DataContract]
    public sealed class DownloadHistoryDocument
    {
        public DownloadHistoryDocument()
        {
            Items = new List<DownloadRecord>();
        }

        [DataMember(Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 1)]
        public List<DownloadRecord> Items { get; set; }

        public static DownloadHistoryDocument CreateDefault()
        {
            return new DownloadHistoryDocument
            {
                SchemaVersion = BrowserDefaults.DataSchemaVersion,
                Items = new List<DownloadRecord>()
            };
        }

        internal DownloadHistoryDocument DeepClone()
        {
            var clone = new DownloadHistoryDocument { SchemaVersion = SchemaVersion };
            foreach (var item in Items ?? new List<DownloadRecord>())
            {
                if (item != null)
                {
                    clone.Items.Add(item.Clone());
                }
            }
            return clone;
        }
    }
}
