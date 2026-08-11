using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Win32;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Models
{
    public enum StartupMode
    {
        Home = 0,
        LastActivePage = 1
    }

    [DataContract]
    public sealed class BrowserSettings
    {
        [DataMember(Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 1)]
        public string UiCulture { get; set; }

        [DataMember(Order = 2)]
        public string HomeUrl { get; set; }

        [DataMember(Order = 3)]
        public string SearchUrlTemplate { get; set; }

        [DataMember(Order = 4)]
        public StartupMode StartupMode { get; set; }

        [DataMember(Order = 5)]
        public string DownloadDirectory { get; set; }

        [DataMember(Order = 6)]
        public bool ShowLinksBar { get; set; }

        [DataMember(Order = 7)]
        public bool ShowStatusBar { get; set; }

        public static BrowserSettings CreateDefault()
        {
            return new BrowserSettings
            {
                SchemaVersion = BrowserDefaults.DataSchemaVersion,
                UiCulture = BrowserDefaults.UiCultureName,
                HomeUrl = BrowserDefaults.HomeUrl,
                SearchUrlTemplate = BrowserDefaults.SearchUrlTemplate,
                StartupMode = StartupMode.Home,
                DownloadDirectory = KnownFolders.GetDownloadsDirectory(),
                ShowLinksBar = false,
                ShowStatusBar = true
            };
        }

        public BrowserSettings Clone()
        {
            return new BrowserSettings
            {
                SchemaVersion = SchemaVersion,
                UiCulture = UiCulture,
                HomeUrl = HomeUrl,
                SearchUrlTemplate = SearchUrlTemplate,
                StartupMode = StartupMode,
                DownloadDirectory = DownloadDirectory,
                ShowLinksBar = ShowLinksBar,
                ShowStatusBar = ShowStatusBar
            };
        }
    }

    internal static class KnownFolders
    {
        public static string GetDownloadsDirectory()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    WindowsIntegrationConstants.UserShellFoldersRegistryPath))
                {
                    var value = key?.GetValue(WindowsIntegrationConstants.DownloadsFolderId) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return Environment.ExpandEnvironmentVariables(value);
                    }
                }
            }
            catch
            {
                // Fall back to the conventional path when the shell setting is unavailable.
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                WindowsIntegrationConstants.DownloadsFallbackDirectoryName);
        }
    }
}
