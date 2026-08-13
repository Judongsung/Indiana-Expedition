using System;
using System.Threading.Tasks;

namespace IndianaExpedition
{
    internal interface IPageFindController : IDisposable
    {
        event EventHandler StateChanged;

        int ActiveMatchIndex { get; }

        int MatchCount { get; }

        PageFindCriteria CurrentCriteria { get; }

        Task FindAsync(PageFindCriteria criteria);

        Task RepeatAsync(bool previous);

        void ResetSession();
    }
}
