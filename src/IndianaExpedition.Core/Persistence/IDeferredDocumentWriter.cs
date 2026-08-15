using System;
using System.Threading.Tasks;

namespace IndianaExpedition.Core.Persistence
{
    internal interface IDeferredDocumentWriter<T> : IDisposable where T : class
    {
        event EventHandler<PersistenceWriteFailedEventArgs> PersistenceWriteFailed;

        void Schedule(T snapshot);

        Task<bool> SaveImmediatelyAsync(T snapshot, TimeSpan timeout);

        Task<bool> FlushAsync(TimeSpan timeout);
    }
}
