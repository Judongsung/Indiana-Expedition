using IndianaExpedition.BrowsingData;

namespace IndianaExpedition.Constants
{
    internal static class UiAutomationIds
    {
        internal static class Browser
        {
            internal const string MainMenu = "Browser.MainMenu";
            internal const string FavoritesMenu = "Browser.FavoritesMenu";
            internal const string HelpMenu = "Browser.HelpMenu";
            internal const string FavoritesSidebarButton = "Browser.FavoritesSidebarButton";
            internal const string HistorySidebarButton = "Browser.HistorySidebarButton";
            internal const string ContentSplit = "Browser.ContentSplit";
            internal const string InformationBar = "Browser.InformationBar";
            internal const string OpenBlockedPopupButton = "Browser.OpenBlockedPopupButton";
            internal const string AllowPopupOriginButton = "Browser.AllowPopupOriginButton";
            internal const string CloseInformationBarButton = "Browser.CloseInformationBarButton";
        }

        internal static class DownloadHistory
        {
            internal const string CloseButton = "DownloadHistory.CloseButton";
        }

        internal static class PageFind
        {
            internal const string Term = "PageFind.Term";
            internal const string FindNextButton = "PageFind.FindNextButton";
        }

        internal static class BrowsingData
        {
            internal const string DeleteButton = "BrowsingData.DeleteButton";
            internal const string CancelButton = "BrowsingData.CancelButton";
            private const string OptionPrefix = "BrowsingData.Option.";

            internal static string Option(BrowsingDataSelection selection)
            {
                return OptionPrefix + selection;
            }
        }

        internal static class ContextMenu
        {
            private const string CommandPrefix = "ContextMenu.Command.";

            internal static string Command(IndianaExpedition.ContextMenus.PageContextMenuCommand command)
            {
                return CommandPrefix + command;
            }
        }
    }
}
