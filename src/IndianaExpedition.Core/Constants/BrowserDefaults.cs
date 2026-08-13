namespace IndianaExpedition.Core.Constants
{
    public static class BrowserDefaults
    {
        public const int DataSchemaVersion = 2;
        public const string UiCultureName = "ko-KR";
        public const string HomeUrl = "https://www.google.com/";
        public const string SearchUrlTemplate = "https://www.google.com/search?q={query}";
        public const string BlankPageUrl = "about:blank";
    }

    public static class NavigationConstants
    {
        public const string SearchQueryToken = "{query}";
        public const string SearchTemplateProbe = "test";
        public const string HttpsPrefix = "https://";
        public const string SchemeSeparator = "://";
        public const string Localhost = "localhost";
        public const string AboutScheme = "about";
        public const string JavaScriptScheme = "javascript";
        public const string DataScheme = "data";
        public const string VbScriptScheme = "vbscript";
    }

    public static class HistoryPolicy
    {
        public const int RetentionDays = 30;
        public const int MaximumEntries = 2000;
    }

    internal static class PopupPolicyConstants
    {
        internal const int PopupSettingsSchemaVersion = 2;
        internal const int MaximumAllowedOrigins = 200;
    }
}
