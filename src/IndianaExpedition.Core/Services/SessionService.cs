using System;
using System.Threading.Tasks;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Services
{
    public sealed class SessionService : IFlushablePersistence, IDisposable
    {
        private readonly object _gate = new object();
        private readonly IDeferredDocumentWriter<SessionState> _writer;
        private SessionState _current;

        public SessionService(string path)
            : this(new AtomicJsonFileStore<SessionState>(path, SessionState.CreateDefault))
        {
        }

        internal SessionService(IDocumentStore<SessionState> store)
            : this(store, new DebouncedDocumentWriter<SessionState>(store, state => state.Clone()))
        {
        }

        internal SessionService(
            IDocumentStore<SessionState> store,
            IDeferredDocumentWriter<SessionState> writer)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _writer.PersistenceWriteFailed += OnPersistenceWriteFailed;
            _current = store.Load() ?? SessionState.CreateDefault();
            _current.SchemaVersion = BrowserDefaults.DataSchemaVersion;
        }

        public SessionState Current
        {
            get
            {
                lock (_gate)
                {
                    return _current.Clone();
                }
            }
        }

        internal event EventHandler<PersistenceWriteFailedEventArgs> PersistenceWriteFailed;

        event EventHandler<PersistenceWriteFailedEventArgs> IFlushablePersistence.PersistenceWriteFailed
        {
            add { PersistenceWriteFailed += value; }
            remove { PersistenceWriteFailed -= value; }
        }

        public void Remember(string url)
        {
            if (!AddressResolver.IsHistoryEligible(url) &&
                !string.Equals(url, BrowserDefaults.BlankPageUrl, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SessionState snapshot;
            lock (_gate)
            {
                if (string.Equals(_current.LastActiveUrl, url, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                _current.LastActiveUrl = url;
                snapshot = _current.Clone();
            }
            _writer.Schedule(snapshot);
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

        private void OnPersistenceWriteFailed(object sender, PersistenceWriteFailedEventArgs args)
        {
            PersistenceWriteFailed?.Invoke(this, args);
        }
    }
}
