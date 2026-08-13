using System;
using System.IO;
using System.Linq;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Services
{
    public sealed class SettingsService
    {
        private readonly object _gate = new object();
        private readonly AtomicJsonFileStore<BrowserSettings> _store;
        private BrowserSettings _current;

        public SettingsService(string path)
        {
            _store = new AtomicJsonFileStore<BrowserSettings>(path, BrowserSettings.CreateDefault);
            var loaded = _store.Load();
            var saveNormalizedFeatures = RequiresFeatureSettingsSave(loaded);
            _current = Normalize(loaded);
            if (saveNormalizedFeatures)
            {
                _store.Save(_current);
            }
        }

        public event EventHandler Changed;

        public BrowserSettings Current
        {
            get
            {
                lock (_gate)
                {
                    return _current.Clone();
                }
            }
        }

        public void Update(Action<BrowserSettings> update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            lock (_gate)
            {
                var candidate = _current.Clone();
                update(candidate);
                _current = Normalize(candidate);
                _store.Save(_current);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static BrowserSettings Normalize(BrowserSettings settings)
        {
            var defaults = BrowserSettings.CreateDefault();
            var result = settings ?? defaults;
            var sourceSchemaVersion = result.SchemaVersion;

            result.SchemaVersion = BrowserDefaults.DataSchemaVersion;
            result.UiCulture = string.IsNullOrWhiteSpace(result.UiCulture)
                ? defaults.UiCulture
                : result.UiCulture.Trim();
            result.HomeUrl = IsValidHomeUrl(result.HomeUrl)
                ? result.HomeUrl.Trim()
                : defaults.HomeUrl;
            result.SearchUrlTemplate = IsValidSearchTemplate(result.SearchUrlTemplate)
                ? result.SearchUrlTemplate.Trim()
                : defaults.SearchUrlTemplate;

            if (!Enum.IsDefined(typeof(StartupMode), result.StartupMode))
            {
                result.StartupMode = defaults.StartupMode;
            }

            if (sourceSchemaVersion < PopupPolicyConstants.PopupSettingsSchemaVersion)
            {
                result.PopupBlockerEnabled = defaults.PopupBlockerEnabled;
                result.DefaultZoomLevel = defaults.DefaultZoomLevel;
            }

            result.AllowedPopupOrigins = PopupPolicy.NormalizeOrigins(result.AllowedPopupOrigins);
            if (!Enum.IsDefined(typeof(BrowserZoomLevel), result.DefaultZoomLevel))
            {
                result.DefaultZoomLevel = defaults.DefaultZoomLevel;
            }

            if (string.IsNullOrWhiteSpace(result.DownloadDirectory))
            {
                result.DownloadDirectory = defaults.DownloadDirectory;
            }
            else
            {
                try
                {
                    result.DownloadDirectory = Path.GetFullPath(result.DownloadDirectory);
                }
                catch
                {
                    result.DownloadDirectory = defaults.DownloadDirectory;
                }
            }

            return result;
        }

        private static bool IsValidHomeUrl(string value)
        {
            if (string.Equals(value?.Trim(), BrowserDefaults.BlankPageUrl, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private static bool RequiresFeatureSettingsSave(BrowserSettings settings)
        {
            if (settings == null ||
                settings.SchemaVersion != BrowserDefaults.DataSchemaVersion ||
                !Enum.IsDefined(typeof(BrowserZoomLevel), settings.DefaultZoomLevel) ||
                settings.AllowedPopupOrigins == null)
            {
                return true;
            }

            var normalizedOrigins = PopupPolicy.NormalizeOrigins(settings.AllowedPopupOrigins);
            return !settings.AllowedPopupOrigins.SequenceEqual(
                normalizedOrigins,
                StringComparer.Ordinal);
        }

        private static bool IsValidSearchTemplate(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains(NavigationConstants.SearchQueryToken))
            {
                return false;
            }

            return Uri.TryCreate(
                       value.Replace(NavigationConstants.SearchQueryToken, NavigationConstants.SearchTemplateProbe),
                       UriKind.Absolute,
                       out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
