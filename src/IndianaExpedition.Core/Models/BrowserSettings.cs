using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Models
{
    public enum StartupMode
    {
        Home = 0,
        LastActivePage = 1
    }

    public enum BrowserZoomLevel
    {
        Smallest = 0,
        Smaller = 1,
        Medium = 2,
        Larger = 3,
        Largest = 4
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

        [DataMember(Order = 8)]
        public bool PopupBlockerEnabled { get; set; }

        [DataMember(Order = 9)]
        public List<string> AllowedPopupOrigins { get; set; }

        [DataMember(Order = 10)]
        public BrowserZoomLevel DefaultZoomLevel { get; set; }

        [DataMember(Order = 11)]
        public bool AskWhereToSaveDownloads { get; set; }

        public static BrowserSettings CreateDefault()
        {
            return new BrowserSettings
            {
                SchemaVersion = BrowserDefaults.DataSchemaVersion,
                UiCulture = BrowserDefaults.UiCultureName,
                HomeUrl = BrowserDefaults.HomeUrl,
                SearchUrlTemplate = BrowserDefaults.SearchUrlTemplate,
                StartupMode = StartupMode.Home,
                DownloadDirectory = GetConventionalDownloadsDirectory(),
                ShowLinksBar = false,
                ShowStatusBar = true,
                PopupBlockerEnabled = true,
                AllowedPopupOrigins = new List<string>(),
                DefaultZoomLevel = BrowserZoomLevel.Medium,
                AskWhereToSaveDownloads = false
            };
        }

        private static string GetConventionalDownloadsDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                WindowsIntegrationConstants.DownloadsFallbackDirectoryName);
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
                ShowStatusBar = ShowStatusBar,
                PopupBlockerEnabled = PopupBlockerEnabled,
                AllowedPopupOrigins = new List<string>(AllowedPopupOrigins ?? new List<string>()),
                DefaultZoomLevel = DefaultZoomLevel,
                AskWhereToSaveDownloads = AskWhereToSaveDownloads
            };
        }
    }

}
