using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IndianaExpedition.Core;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Persistence;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition
{
    internal sealed class BrowserApplicationServices : IDisposable
    {
        private bool _persistenceFailurePublished;
        private bool _disposed;

        internal BrowserApplicationServices(AppDataPaths paths)
        {
            Paths = paths ?? throw new ArgumentNullException(nameof(paths));
            Paths.EnsureDirectories();
            Settings = new SettingsService(
                new AtomicJsonFileStore<BrowserSettings>(
                    paths.SettingsFile,
                    WindowsBrowserSettingsDefaultsProvider.CreateDefault),
                WindowsBrowserSettingsDefaultsProvider.CreateDefault);
            Favorites = new FavoritesService(paths.FavoritesFile);
            History = new HistoryService(paths.HistoryFile);
            Downloads = new DownloadHistoryService(paths.DownloadHistoryFile);
            Session = new SessionService(paths.SessionFile);
            History.PersistenceWriteFailed += OnPersistenceWriteFailed;
            Session.PersistenceWriteFailed += OnPersistenceWriteFailed;
        }

        internal event EventHandler<PersistenceWriteFailedEventArgs> PersistenceWriteFailed;

        internal AppDataPaths Paths { get; }
        internal SettingsService Settings { get; }
        internal FavoritesService Favorites { get; }
        internal HistoryService History { get; }
        internal DownloadHistoryService Downloads { get; }
        internal SessionService Session { get; }

        internal async Task<bool> FlushAsync(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            if (!await History.FlushAsync(Remaining(deadline)).ConfigureAwait(false))
            {
                return false;
            }
            return await Session.FlushAsync(Remaining(deadline)).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (!FlushAsync(TimeSpan.FromMilliseconds(PersistencePolicy.ShutdownFlushTimeoutMilliseconds))
                    .GetAwaiter()
                    .GetResult())
                {
                    Trace.TraceError("Session/history shutdown flush exceeded the configured timeout.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

            History.PersistenceWriteFailed -= OnPersistenceWriteFailed;
            Session.PersistenceWriteFailed -= OnPersistenceWriteFailed;
            History.Dispose();
            Session.Dispose();
            _disposed = true;
        }

        private void OnPersistenceWriteFailed(object sender, PersistenceWriteFailedEventArgs args)
        {
            Trace.TraceError(args.Exception.ToString());
            if (_persistenceFailurePublished)
            {
                return;
            }
            _persistenceFailurePublished = true;
            PersistenceWriteFailed?.Invoke(this, args);
        }

        private static TimeSpan Remaining(DateTime deadline)
        {
            var remaining = deadline - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.FromTicks(1);
        }
    }
}
