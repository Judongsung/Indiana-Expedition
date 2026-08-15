using System;
using System.Collections.Generic;
using System.Linq;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Constants
{
    internal static class RecentAddressConstants
    {
        internal const int MaximumEntries = 100;
    }

    internal sealed class BrowserZoomDefinition
    {
        internal BrowserZoomDefinition(
            BrowserZoomLevel level,
            double factor,
            Func<string> getText)
        {
            Level = level;
            Factor = factor;
            Percentage = (int)Math.Round(factor * 100d, MidpointRounding.AwayFromZero);
            GetText = getText;
        }

        internal BrowserZoomLevel Level { get; }
        internal double Factor { get; }
        internal int Percentage { get; }
        internal Func<string> GetText { get; }
    }

    internal static class BrowserZoomCatalog
    {
        internal const double FactorComparisonTolerance = 0.001d;

        internal static readonly IReadOnlyList<BrowserZoomDefinition> Ordered =
            new[]
            {
                new BrowserZoomDefinition(BrowserZoomLevel.Smallest, 0.67d, () => Strings.TextSizeSmallest),
                new BrowserZoomDefinition(BrowserZoomLevel.Smaller, 0.80d, () => Strings.TextSizeSmaller),
                new BrowserZoomDefinition(BrowserZoomLevel.Medium, 1.00d, () => Strings.TextSizeMedium),
                new BrowserZoomDefinition(BrowserZoomLevel.Larger, 1.25d, () => Strings.TextSizeLarger),
                new BrowserZoomDefinition(BrowserZoomLevel.Largest, 1.50d, () => Strings.TextSizeLargest)
            };

        private static readonly IReadOnlyDictionary<BrowserZoomLevel, BrowserZoomDefinition> ByLevel =
            Ordered.ToDictionary(item => item.Level);

        private static readonly IReadOnlyDictionary<BrowserZoomLevel, int> Indices =
            Ordered.Select((item, index) => new { item.Level, Index = index })
                .ToDictionary(item => item.Level, item => item.Index);

        internal static BrowserZoomDefinition Get(BrowserZoomLevel level)
        {
            return ByLevel.TryGetValue(level, out var definition)
                ? definition
                : ByLevel[BrowserZoomLevel.Medium];
        }

        internal static BrowserZoomLevel Step(BrowserZoomLevel level, int direction)
        {
            var normalized = Get(level).Level;
            var index = Indices[normalized];
            index = Math.Max(0, Math.Min(Ordered.Count - 1, index + Math.Sign(direction)));
            return Ordered[index].Level;
        }
    }

    internal static class WebViewRuntimeConstants
    {
        internal const string MinimumVersion = "139.0.3405.78";
    }
}
