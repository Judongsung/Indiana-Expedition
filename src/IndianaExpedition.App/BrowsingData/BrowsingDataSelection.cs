using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.BrowsingData
{
    [Flags]
    internal enum BrowsingDataSelection
    {
        None = 0,
        History = 1 << 0,
        DownloadHistory = 1 << 1,
        DiskCache = 1 << 2,
        Cookies = 1 << 3,
        SiteStorage = 1 << 4,
        Autofill = 1 << 5,
        Passwords = 1 << 6,
        SitePermissions = 1 << 7,
        SafeDefaults = History | DownloadHistory | DiskCache | Cookies | SiteStorage
    }

    internal static class BrowsingDataMapper
    {
        private static readonly IReadOnlyDictionary<BrowsingDataSelection, CoreWebView2BrowsingDataKinds>
            WebViewKindsBySelection =
                new ReadOnlyDictionary<BrowsingDataSelection, CoreWebView2BrowsingDataKinds>(
                    new Dictionary<BrowsingDataSelection, CoreWebView2BrowsingDataKinds>
                    {
                        [BrowsingDataSelection.History] = CoreWebView2BrowsingDataKinds.BrowsingHistory,
                        [BrowsingDataSelection.DownloadHistory] = CoreWebView2BrowsingDataKinds.DownloadHistory,
                        [BrowsingDataSelection.DiskCache] = CoreWebView2BrowsingDataKinds.DiskCache,
                        [BrowsingDataSelection.Cookies] = CoreWebView2BrowsingDataKinds.Cookies,
                        [BrowsingDataSelection.SiteStorage] = CoreWebView2BrowsingDataKinds.AllDomStorage |
                                                              CoreWebView2BrowsingDataKinds.ServiceWorkers,
                        [BrowsingDataSelection.Autofill] = CoreWebView2BrowsingDataKinds.GeneralAutofill,
                        [BrowsingDataSelection.Passwords] = CoreWebView2BrowsingDataKinds.PasswordAutosave
                    });

        internal static CoreWebView2BrowsingDataKinds ToWebViewKinds(BrowsingDataSelection selection)
        {
            var kinds = (CoreWebView2BrowsingDataKinds)0;

            foreach (var mapping in WebViewKindsBySelection)
            {
                if ((selection & mapping.Key) == mapping.Key)
                {
                    kinds |= mapping.Value;
                }
            }

            return kinds;
        }
    }
}
