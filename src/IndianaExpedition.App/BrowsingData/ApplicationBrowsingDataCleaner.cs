using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;

namespace IndianaExpedition
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
                            history.Clear),
                        [BrowsingDataSelection.DownloadHistory] = new ClearDefinition(
                            () => Strings.DownloadHistoryItem,
                            downloads.Clear)
                    });
        }

        internal IReadOnlyList<string> Clear(BrowsingDataSelection selection)
        {
            var failures = new List<string>();
            foreach (var definition in _definitions
                .Where(item => selection.HasFlag(item.Key))
                .Select(item => item.Value))
            {
                try
                {
                    definition.Clear();
                }
                catch (Exception ex)
                {
                    failures.Add(definition.GetDisplayName() + ": " + ex.Message);
                }
            }
            return failures;
        }

        private sealed class ClearDefinition
        {
            internal ClearDefinition(Func<string> getDisplayName, Action clear)
            {
                GetDisplayName = getDisplayName ??
                    throw new ArgumentNullException(nameof(getDisplayName));
                Clear = clear ?? throw new ArgumentNullException(nameof(clear));
            }

            internal Func<string> GetDisplayName { get; }

            internal Action Clear { get; }
        }
    }
}
