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
        private readonly IDocumentStore<BrowserSettings> _store;
        private readonly Func<BrowserSettings> _defaultsFactory;
        private BrowserSettings _current;

        public SettingsService(string path)
            : this(
                new AtomicJsonFileStore<BrowserSettings>(path, BrowserSettings.CreateDefault),
                BrowserSettings.CreateDefault)
        {
        }

        internal SettingsService(
            IDocumentStore<BrowserSettings> store,
            Func<BrowserSettings> defaultsFactory)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _defaultsFactory = defaultsFactory ?? throw new ArgumentNullException(nameof(defaultsFactory));
            var normalized = Normalize(_store.Load(), out var changed);
            if (changed)
            {
                _store.Save(normalized);
            }
            _current = normalized;
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
                candidate = Normalize(candidate, out _);
                _store.Save(candidate);
                _current = candidate;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private BrowserSettings Normalize(BrowserSettings settings, out bool changed)
        {
            var defaults = _defaultsFactory();
            var original = settings?.Clone();
            var result = settings?.Clone() ?? defaults.Clone();
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

            changed = original == null || !SettingsEqual(original, result);
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

        private static bool SettingsEqual(BrowserSettings left, BrowserSettings right)
        {
            return left.SchemaVersion == right.SchemaVersion &&
                   string.Equals(left.UiCulture, right.UiCulture, StringComparison.Ordinal) &&
                   string.Equals(left.HomeUrl, right.HomeUrl, StringComparison.Ordinal) &&
                   string.Equals(left.SearchUrlTemplate, right.SearchUrlTemplate, StringComparison.Ordinal) &&
                   left.StartupMode == right.StartupMode &&
                   string.Equals(left.DownloadDirectory, right.DownloadDirectory, StringComparison.Ordinal) &&
                   left.ShowLinksBar == right.ShowLinksBar &&
                   left.ShowStatusBar == right.ShowStatusBar &&
                   left.PopupBlockerEnabled == right.PopupBlockerEnabled &&
                   (left.AllowedPopupOrigins ?? new System.Collections.Generic.List<string>()).SequenceEqual(
                       right.AllowedPopupOrigins ?? new System.Collections.Generic.List<string>(),
                       StringComparer.Ordinal) &&
                   left.DefaultZoomLevel == right.DefaultZoomLevel &&
                   left.AskWhereToSaveDownloads == right.AskWhereToSaveDownloads;
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
