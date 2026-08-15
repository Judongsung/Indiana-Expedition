using System;
using System.Threading;
using System.Threading.Tasks;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Persistence
{
    internal sealed class DebouncedDocumentWriter<T> : IDeferredDocumentWriter<T>
        where T : class
    {
        private static readonly int[] RetryDelays =
        {
            PersistencePolicy.FirstRetryDelayMilliseconds,
            PersistencePolicy.SecondRetryDelayMilliseconds,
            PersistencePolicy.ThirdRetryDelayMilliseconds
        };

        private readonly object _gate = new object();
        private readonly IDocumentStore<T> _store;
        private readonly Func<T, T> _clone;
        private readonly Func<int, Task> _delay;
        private readonly Timer _timer;
        private T _latest;
        private long _version;
        private long _failedVersion = -1;
        private bool _dirty;
        private bool _failurePublished;
        private bool _disposed;
        private Task _activeWrite = Task.CompletedTask;

        internal DebouncedDocumentWriter(IDocumentStore<T> store, Func<T, T> clone)
            : this(store, clone, milliseconds => Task.Delay(milliseconds))
        {
        }

        internal DebouncedDocumentWriter(
            IDocumentStore<T> store,
            Func<T, T> clone,
            Func<int, Task> delay)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clone = clone ?? throw new ArgumentNullException(nameof(clone));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        public event EventHandler<PersistenceWriteFailedEventArgs> PersistenceWriteFailed;

        public void Schedule(T snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            lock (_gate)
            {
                ThrowIfDisposed();
                SetLatestLocked(snapshot);
                _timer.Change(PersistencePolicy.DebounceMilliseconds, Timeout.Infinite);
            }
        }

        public Task<bool> SaveImmediatelyAsync(T snapshot, TimeSpan timeout)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            lock (_gate)
            {
                ThrowIfDisposed();
                SetLatestLocked(snapshot);
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            return FlushAsync(timeout);
        }

        public async Task<bool> FlushAsync(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                return false;
            }

            var deadline = DateTime.UtcNow + timeout;
            var attempted = false;
            while (true)
            {
                Task activeWrite;
                lock (_gate)
                {
                    ThrowIfDisposed();
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    if (!_dirty && _activeWrite.IsCompleted)
                    {
                        return true;
                    }

                    if (!attempted && _failedVersion == _version)
                    {
                        _failedVersion = -1;
                    }
                    else if (attempted && _failedVersion == _version && _activeWrite.IsCompleted)
                    {
                        return false;
                    }

                    StartWriteLocked();
                    activeWrite = _activeWrite;
                    attempted = true;
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    return false;
                }

                if (await Task.WhenAny(activeWrite, Task.Delay(remaining)).ConfigureAwait(false) != activeWrite)
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _timer.Dispose();
            }
        }

        private void OnTimerElapsed(object state)
        {
            lock (_gate)
            {
                if (_disposed || !_dirty)
                {
                    return;
                }

                if (!_activeWrite.IsCompleted)
                {
                    _timer.Change(PersistencePolicy.DebounceMilliseconds, Timeout.Infinite);
                    return;
                }

                StartWriteLocked();
            }
        }

        private void SetLatestLocked(T snapshot)
        {
            _latest = _clone(snapshot);
            _version++;
            _dirty = true;
        }

        private void StartWriteLocked()
        {
            if (!_activeWrite.IsCompleted || !_dirty || _latest == null)
            {
                return;
            }

            var snapshot = _clone(_latest);
            var version = _version;
            _activeWrite = PersistAsync(snapshot, version);
        }

        private async Task PersistAsync(T snapshot, long version)
        {
            Exception lastException = null;
            for (var attempt = 0; attempt < PersistencePolicy.MaximumSaveAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    lock (_gate)
                    {
                        if (_disposed || version != _version)
                        {
                            return;
                        }
                    }

                    await _delay(RetryDelays[attempt - 1]).ConfigureAwait(false);
                }

                try
                {
                    await Task.Run(() => _store.Save(snapshot)).ConfigureAwait(false);
                    lock (_gate)
                    {
                        if (version == _version)
                        {
                            _dirty = false;
                        }
                        else if (!_disposed)
                        {
                            _timer.Change(0, Timeout.Infinite);
                        }
                        _failurePublished = false;
                        _failedVersion = -1;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            EventHandler<PersistenceWriteFailedEventArgs> handler = null;
            lock (_gate)
            {
                _failedVersion = version;
                if (!_failurePublished)
                {
                    _failurePublished = true;
                    handler = PersistenceWriteFailed;
                }
                if (version != _version && !_disposed)
                {
                    _timer.Change(0, Timeout.Infinite);
                }
            }

            handler?.Invoke(
                this,
                new PersistenceWriteFailedEventArgs(
                    lastException ?? new InvalidOperationException("The document could not be saved."),
                    PersistencePolicy.MaximumSaveAttempts));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
