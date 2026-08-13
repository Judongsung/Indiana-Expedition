using System;
using IndianaExpedition.Core.Models;

namespace IndianaExpedition.Core.Navigation
{
    internal static class BrowserZoomPolicy
    {
        internal static BrowserZoomLevel Normalize(BrowserZoomLevel level)
        {
            return Enum.IsDefined(typeof(BrowserZoomLevel), level)
                ? level
                : BrowserZoomLevel.Medium;
        }

        internal static BrowserZoomLevel Step(BrowserZoomLevel level, int direction)
        {
            var normalized = Normalize(level);
            var candidate = (int)normalized + Math.Sign(direction);
            candidate = Math.Max((int)BrowserZoomLevel.Smallest, candidate);
            candidate = Math.Min((int)BrowserZoomLevel.Largest, candidate);
            return (BrowserZoomLevel)candidate;
        }
    }
}
