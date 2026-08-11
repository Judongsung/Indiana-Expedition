namespace IndianaExpedition.Core.Constants
{
    public static class StorageConstants
    {
        public const string DataDirectoryName = "Data";
        public const string WebView2DirectoryName = "WebView2";
        public const string SettingsFileName = "settings.json";
        public const string FavoritesFileName = "favorites.json";
        public const string HistoryFileName = "history.json";
        public const string SessionFileName = "session.json";
        public const string TemporaryFileSuffix = ".tmp";
        public const string BackupFileSuffix = ".bak";
        public const string CorruptFileMarker = ".corrupt-";
        public const string BackupTimestampFormat = "yyyyMMdd-HHmmssfff";
    }

    public static class WindowsIntegrationConstants
    {
        public const string DownloadsFolderId = "{374DE290-123F-4565-9164-39C4925E467B}";
        public const string UserShellFoldersRegistryPath =
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders";
        public const string DownloadsFallbackDirectoryName = "Downloads";
    }
}
