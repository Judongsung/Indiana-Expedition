using System;
using System.Collections.Generic;
using System.Linq;

namespace IndianaExpedition.VisualTestHost
{
    internal static class VisualStateRegistry
    {
        internal static readonly IReadOnlyList<string> Names = new[]
        {
            "Main",
            "Favorites",
            "History",
            "PopupBlocked",
            "FindDialog",
            "DeleteBrowsingDataDialog",
            "DownloadProgressDialog",
            "DownloadCompletedDialog",
            "DownloadHistoryDialog",
            "PermissionRequestDialog",
            "PrivacyTab",
            "ContextMenu",
            "HelpMenu",
            "AboutDialog"
        };

        internal static bool Contains(string value)
        {
            return Names.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        internal static string ToJson()
        {
            return "[" + string.Join(",", Names.Select(name => "\"" + name + "\"")) + "]";
        }
    }
}
