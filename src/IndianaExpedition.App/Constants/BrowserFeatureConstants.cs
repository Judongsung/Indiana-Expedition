using System;
using IndianaExpedition.Core.Models;

namespace IndianaExpedition.Constants
{
    internal static class BrowserZoomConstants
    {
        internal const double SmallestFactor = 0.67d;
        internal const double SmallerFactor = 0.80d;
        internal const double MediumFactor = 1.00d;
        internal const double LargerFactor = 1.25d;
        internal const double LargestFactor = 1.50d;
        internal const double FactorComparisonTolerance = 0.001d;

        internal static double GetFactor(BrowserZoomLevel level)
        {
            switch (level)
            {
                case BrowserZoomLevel.Smallest:
                    return SmallestFactor;
                case BrowserZoomLevel.Smaller:
                    return SmallerFactor;
                case BrowserZoomLevel.Larger:
                    return LargerFactor;
                case BrowserZoomLevel.Largest:
                    return LargestFactor;
                default:
                    return MediumFactor;
            }
        }

        internal static int GetPercentage(BrowserZoomLevel level)
        {
            return (int)Math.Round(GetFactor(level) * 100d, MidpointRounding.AwayFromZero);
        }
    }

    internal static class WebViewRuntimeConstants
    {
        internal const string MinimumVersion = "139.0.3405.78";
    }
}
