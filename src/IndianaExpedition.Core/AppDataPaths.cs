using System;
using System.IO;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core
{
    public sealed class AppDataPaths
    {
        public AppDataPaths(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException(CoreMessages.BaseDirectoryRequired, nameof(baseDirectory));
            }

            BaseDirectory = Path.GetFullPath(baseDirectory);
            DataDirectory = Path.Combine(BaseDirectory, StorageConstants.DataDirectoryName);
            WebView2Directory = Path.Combine(BaseDirectory, StorageConstants.WebView2DirectoryName);
            SettingsFile = Path.Combine(DataDirectory, StorageConstants.SettingsFileName);
            FavoritesFile = Path.Combine(DataDirectory, StorageConstants.FavoritesFileName);
            HistoryFile = Path.Combine(DataDirectory, StorageConstants.HistoryFileName);
            DownloadHistoryFile = Path.Combine(DataDirectory, StorageConstants.DownloadHistoryFileName);
            SessionFile = Path.Combine(DataDirectory, StorageConstants.SessionFileName);
        }

        public string BaseDirectory { get; }

        public string DataDirectory { get; }

        public string WebView2Directory { get; }

        public string SettingsFile { get; }

        public string FavoritesFile { get; }

        public string HistoryFile { get; }

        public string DownloadHistoryFile { get; }

        public string SessionFile { get; }

        public static AppDataPaths CreateDefault(string applicationDirectoryName)
        {
            if (string.IsNullOrWhiteSpace(applicationDirectoryName))
            {
                throw new ArgumentException(CoreMessages.ApplicationDirectoryNameRequired, nameof(applicationDirectoryName));
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return new AppDataPaths(Path.Combine(localAppData, applicationDirectoryName));
        }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(BaseDirectory);
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(WebView2Directory);
        }
    }
}
