namespace IndianaExpedition.Core.Constants
{
    public static class StorageConstants
    {
        public const string DataDirectoryName = "Data";
        public const string WebView2DirectoryName = "WebView2";
        public const string SettingsFileName = "settings.json";
        public const string FavoritesFileName = "favorites.json";
        public const string HistoryFileName = "history.json";
        public const string DownloadHistoryFileName = "downloads.json";
        public const string SessionFileName = "session.json";
        public const string TemporaryFileSuffix = ".tmp";
        public const string BackupFileSuffix = ".bak";
        public const string CorruptFileMarker = ".corrupt-";
        public const string BackupTimestampFormat = "yyyyMMdd-HHmmssfff";
        public const string CompactIdentifierFormat = "N";
    }

    internal static class PersistencePolicy
    {
        internal const int DebounceMilliseconds = 500;
        internal const int FirstRetryDelayMilliseconds = 250;
        internal const int SecondRetryDelayMilliseconds = 1000;
        internal const int ThirdRetryDelayMilliseconds = 5000;
        internal const int MaximumSaveAttempts = 4;
        internal const int ShutdownFlushTimeoutMilliseconds = 2000;
    }

    public static class WindowsIntegrationConstants
    {
        public const string DownloadsFolderId = "{374DE290-123F-4565-9164-39C4925E467B}";
        public const string UserShellFoldersRegistryPath =
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders";
        public const string DownloadsFallbackDirectoryName = "Downloads";
    }
}
