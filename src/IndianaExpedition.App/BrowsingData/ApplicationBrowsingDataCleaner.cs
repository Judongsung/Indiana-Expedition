using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;

namespace IndianaExpedition.BrowsingData
{
    internal sealed class ApplicationBrowsingDataCleaner
    {
        private readonly IReadOnlyDictionary<BrowsingDataSelection, ClearDefinition>
            _definitions;

        internal ApplicationBrowsingDataCleaner(
            HistoryService history,
            DownloadHistoryService downloads)
        {
            if (history == null)
            {
                throw new ArgumentNullException(nameof(history));
            }
            if (downloads == null)
            {
                throw new ArgumentNullException(nameof(downloads));
            }

            _definitions =
                new ReadOnlyDictionary<BrowsingDataSelection, ClearDefinition>(
                    new Dictionary<BrowsingDataSelection, ClearDefinition>
                    {
                        [BrowsingDataSelection.History] = new ClearDefinition(
                            () => Strings.BrowsingHistoryItem,
                            history.ClearAsync),
                        [BrowsingDataSelection.DownloadHistory] = new ClearDefinition(
                            () => Strings.DownloadHistoryItem,
                            () => Task.Run((Action)downloads.Clear))
                    });
        }

        internal async Task<IReadOnlyList<string>> ClearAsync(BrowsingDataSelection selection)
        {
            var failures = new List<string>();
            foreach (var definition in _definitions
                .Where(item => (selection & item.Key) != 0)
                .Select(item => item.Value))
            {
                try
                {
                    await definition.ClearAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    failures.Add(string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.BrowsingDataItemDeleteFailedFormat,
                        definition.GetDisplayName()));
                }
            }
            return failures;
        }

        private sealed class ClearDefinition
        {
            internal ClearDefinition(Func<string> getDisplayName, Func<Task> clearAsync)
            {
                GetDisplayName = getDisplayName ??
                    throw new ArgumentNullException(nameof(getDisplayName));
                ClearAsync = clearAsync ?? throw new ArgumentNullException(nameof(clearAsync));
            }

            internal Func<string> GetDisplayName { get; }

            internal Func<Task> ClearAsync { get; }
        }
    }
}
