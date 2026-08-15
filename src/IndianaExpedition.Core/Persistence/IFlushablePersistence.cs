using System;
using System.Threading.Tasks;

namespace IndianaExpedition.Core.Persistence
{
    internal interface IFlushablePersistence
    {
        event EventHandler<PersistenceWriteFailedEventArgs> PersistenceWriteFailed;

        Task<bool> FlushAsync(TimeSpan timeout);
    }
}
