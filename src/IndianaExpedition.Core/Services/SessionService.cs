using System;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Services
{
    public sealed class SessionService
    {
        private readonly object _gate = new object();
        private readonly AtomicJsonFileStore<SessionState> _store;
        private SessionState _current;

        public SessionService(string path)
        {
            _store = new AtomicJsonFileStore<SessionState>(path, SessionState.CreateDefault);
            _current = _store.Load() ?? SessionState.CreateDefault();
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

        public void Remember(string url)
        {
            if (!AddressResolver.IsHistoryEligible(url) &&
                !string.Equals(url, BrowserDefaults.BlankPageUrl, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lock (_gate)
            {
                _current.LastActiveUrl = url;
                _store.Save(_current);
            }
        }
    }
}
