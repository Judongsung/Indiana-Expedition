using System;

namespace IndianaExpedition.Core.Persistence
{
    internal sealed class PersistenceWriteFailedEventArgs : EventArgs
    {
        internal PersistenceWriteFailedEventArgs(Exception exception, int attemptCount)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            AttemptCount = attemptCount;
        }

        internal Exception Exception { get; }

        internal int AttemptCount { get; }
    }
}
