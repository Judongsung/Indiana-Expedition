using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Resources;

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

    internal sealed class BrowsingDataOptionDefinition
    {
        internal BrowsingDataOptionDefinition(
            BrowsingDataSelection selection,
            Func<string> getText,
            bool selectedByDefault,
            bool requiresProfile,
            bool requiresSitePermissions,
            CoreWebView2BrowsingDataKinds webViewKinds)
        {
            Selection = selection;
            GetText = getText;
            SelectedByDefault = selectedByDefault;
            RequiresProfile = requiresProfile;
            RequiresSitePermissions = requiresSitePermissions;
            WebViewKinds = webViewKinds;
        }

        internal BrowsingDataSelection Selection { get; }
        internal Func<string> GetText { get; }
        internal bool SelectedByDefault { get; }
        internal bool RequiresProfile { get; }
        internal bool RequiresSitePermissions { get; }
        internal CoreWebView2BrowsingDataKinds WebViewKinds { get; }
    }

    internal static class BrowsingDataCatalog
    {
        internal static readonly IReadOnlyList<BrowsingDataOptionDefinition> Definitions =
            new[]
            {
                Define(
                    BrowsingDataSelection.History,
                    () => Strings.BrowsingHistoryItem,
                    true,
                    webViewKinds: CoreWebView2BrowsingDataKinds.BrowsingHistory),
                Define(
                    BrowsingDataSelection.DownloadHistory,
                    () => Strings.DownloadHistoryItem,
                    true,
                    webViewKinds: CoreWebView2BrowsingDataKinds.DownloadHistory),
                Define(BrowsingDataSelection.DiskCache, () => Strings.DiskCacheItem, true, true, CoreWebView2BrowsingDataKinds.DiskCache),
                Define(BrowsingDataSelection.Cookies, () => Strings.CookiesItem, true, true, CoreWebView2BrowsingDataKinds.Cookies),
                Define(BrowsingDataSelection.SiteStorage, () => Strings.SiteStorageItem, true, true, CoreWebView2BrowsingDataKinds.AllDomStorage | CoreWebView2BrowsingDataKinds.ServiceWorkers),
                Define(BrowsingDataSelection.Autofill, () => Strings.AutofillItem, false, true, CoreWebView2BrowsingDataKinds.GeneralAutofill),
                Define(BrowsingDataSelection.Passwords, () => Strings.SavedPasswordsItem, false, true, CoreWebView2BrowsingDataKinds.PasswordAutosave),
                new BrowsingDataOptionDefinition(BrowsingDataSelection.SitePermissions, () => Strings.SitePermissionsItem, false, false, true, 0)
            };

        private static BrowsingDataOptionDefinition Define(
            BrowsingDataSelection selection,
            Func<string> getText,
            bool selectedByDefault,
            bool requiresProfile = false,
            CoreWebView2BrowsingDataKinds webViewKinds = 0)
        {
            return new BrowsingDataOptionDefinition(
                selection,
                getText,
                selectedByDefault,
                requiresProfile,
                false,
                webViewKinds);
        }
    }

    internal static class BrowsingDataMapper
    {
        internal static CoreWebView2BrowsingDataKinds ToWebViewKinds(BrowsingDataSelection selection)
        {
            return BrowsingDataCatalog.Definitions
                .Where(definition => (selection & definition.Selection) != 0)
                .Aggregate(
                    (CoreWebView2BrowsingDataKinds)0,
                    (kinds, definition) => kinds | definition.WebViewKinds);
        }
    }
}
