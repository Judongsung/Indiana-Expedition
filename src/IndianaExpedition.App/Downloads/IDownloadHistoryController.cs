using System;
using System.Collections.Generic;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition.Downloads
{
    internal interface IDownloadHistoryController
    {
        event EventHandler Changed;

        IReadOnlyList<DownloadRecord> Items { get; }

        bool Remove(string id);

        void Clear();
    }

    internal sealed class DownloadHistoryController : IDownloadHistoryController
    {
        private readonly DownloadHistoryService _service;

        internal DownloadHistoryController(DownloadHistoryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public event EventHandler Changed
        {
            add => _service.Changed += value;
            remove => _service.Changed -= value;
        }

        public IReadOnlyList<DownloadRecord> Items => _service.Items;

        public bool Remove(string id)
        {
            return _service.Remove(id);
        }

        public void Clear()
        {
            _service.Clear();
        }
    }
}
