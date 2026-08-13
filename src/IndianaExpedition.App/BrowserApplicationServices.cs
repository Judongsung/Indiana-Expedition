using IndianaExpedition.Core;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition
{
    internal sealed class BrowserApplicationServices
    {
        internal BrowserApplicationServices(AppDataPaths paths)
        {
            Paths = paths;
            Paths.EnsureDirectories();
            Settings = new SettingsService(paths.SettingsFile);
            Favorites = new FavoritesService(paths.FavoritesFile);
            History = new HistoryService(paths.HistoryFile);
            Downloads = new DownloadHistoryService(paths.DownloadHistoryFile);
            Session = new SessionService(paths.SessionFile);
        }

        internal AppDataPaths Paths { get; }

        internal SettingsService Settings { get; }

        internal FavoritesService Favorites { get; }

        internal HistoryService History { get; }

        internal DownloadHistoryService Downloads { get; }

        internal SessionService Session { get; }
    }
}
