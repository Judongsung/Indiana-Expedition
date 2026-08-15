using System;
using Microsoft.Win32;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;

namespace IndianaExpedition
{
    internal static class WindowsBrowserSettingsDefaultsProvider
    {
        internal static BrowserSettings CreateDefault()
        {
            var settings = BrowserSettings.CreateDefault();
            settings.DownloadDirectory = GetDownloadsDirectory(settings.DownloadDirectory);
            return settings;
        }

        private static string GetDownloadsDirectory(string fallback)
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
                // The Core-compatible conventional path remains the fallback.
            }
            return fallback;
        }
    }
}
