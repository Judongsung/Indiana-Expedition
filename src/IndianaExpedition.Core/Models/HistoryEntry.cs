using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Models
{
    [DataContract]
    public sealed class HistoryEntry
    {
        [DataMember(Order = 0)]
        public string Url { get; set; }

        [DataMember(Order = 1)]
        public string Title { get; set; }

        [DataMember(Order = 2)]
        public DateTime VisitedAtUtc { get; set; }

        public HistoryEntry Clone()
        {
            return new HistoryEntry
            {
                Url = Url,
                Title = Title,
                VisitedAtUtc = VisitedAtUtc
            };
        }
    }

    [DataContract]
    public sealed class HistoryDocument
    {
        public HistoryDocument()
        {
            Items = new List<HistoryEntry>();
        }

        [DataMember(Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 1)]
        public List<HistoryEntry> Items { get; set; }

        public static HistoryDocument CreateDefault()
        {
            return new HistoryDocument
            {
                SchemaVersion = BrowserDefaults.DataSchemaVersion,
                Items = new List<HistoryEntry>()
            };
        }
    }
}
