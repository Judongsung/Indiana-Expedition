using System;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Navigation
{
    public enum AddressResolutionKind
    {
        Navigate = 0,
        Search = 1,
        ExternalProtocol = 2,
        Blocked = 3
    }

    public sealed class AddressResolution
    {
        public AddressResolution(AddressResolutionKind kind, string target, string errorMessage = null)
        {
            Kind = kind;
            Target = target;
            ErrorMessage = errorMessage;
        }

        public AddressResolutionKind Kind { get; }

        public string Target { get; }

        public string ErrorMessage { get; }
    }

    public static class AddressResolver
    {
        public static AddressResolution Resolve(
            string input,
            string searchUrlTemplate,
            bool allowExplicitFileUri = false)
        {
            var value = (input ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return new AddressResolution(
                    AddressResolutionKind.Blocked,
                    null,
                    CoreMessages.AddressRequired);
            }

            if (string.Equals(value, BrowserDefaults.BlankPageUrl, StringComparison.OrdinalIgnoreCase))
            {
                return new AddressResolution(AddressResolutionKind.Navigate, BrowserDefaults.BlankPageUrl);
            }

            // Uri treats "localhost:5000" as a custom scheme, so host-like input
            // must be normalized before generic absolute-URI handling.
            if (LooksLikeHost(value) &&
                Uri.TryCreate(NavigationConstants.HttpsPrefix + value, UriKind.Absolute, out var inferredHostUri))
            {
                return new AddressResolution(AddressResolutionKind.Navigate, inferredHostUri.AbsoluteUri);
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
            {
                var scheme = absolute.Scheme.ToLowerInvariant();
                if (scheme == Uri.UriSchemeHttp || scheme == Uri.UriSchemeHttps)
                {
                    return new AddressResolution(AddressResolutionKind.Navigate, absolute.AbsoluteUri);
                }

                if (scheme == Uri.UriSchemeFile)
                {
                    return allowExplicitFileUri
                        ? new AddressResolution(AddressResolutionKind.Navigate, absolute.AbsoluteUri)
                        : new AddressResolution(
                            AddressResolutionKind.Blocked,
                            null,
                            CoreMessages.OpenLocalFileFromMenu);
                }

                if (scheme == NavigationConstants.JavaScriptScheme ||
                    scheme == NavigationConstants.DataScheme ||
                    scheme == NavigationConstants.VbScriptScheme)
                {
                    return new AddressResolution(
                        AddressResolutionKind.Blocked,
                        null,
                        CoreMessages.UnsafeAddressBlocked);
                }

                return new AddressResolution(AddressResolutionKind.ExternalProtocol, absolute.AbsoluteUri);
            }

            var template = string.IsNullOrWhiteSpace(searchUrlTemplate) ||
                           !searchUrlTemplate.Contains(NavigationConstants.SearchQueryToken)
                ? BrowserDefaults.SearchUrlTemplate
                : searchUrlTemplate;

            var target = template.Replace(NavigationConstants.SearchQueryToken, Uri.EscapeDataString(value));
            return new AddressResolution(AddressResolutionKind.Search, target);
        }

        public static bool IsHistoryEligible(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private static bool LooksLikeHost(string value)
        {
            if (value.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\\', '@' }) >= 0 ||
                value.Contains(NavigationConstants.SchemeSeparator))
            {
                return false;
            }

            var slashIndex = value.IndexOf('/');
            var hostPort = slashIndex >= 0 ? value.Substring(0, slashIndex) : value;
            if (hostPort.StartsWith(NavigationConstants.Localhost, StringComparison.OrdinalIgnoreCase))
            {
                return hostPort.Length == NavigationConstants.Localhost.Length ||
                       IsNumericPort(hostPort.Substring(NavigationConstants.Localhost.Length));
            }

            var colonIndex = hostPort.LastIndexOf(':');
            if (colonIndex > 0 && !IsNumericPort(hostPort.Substring(colonIndex)))
            {
                return false;
            }

            var host = colonIndex > 0 ? hostPort.Substring(0, colonIndex) : hostPort;
            return host.IndexOf('.') > 0;
        }

        private static bool IsNumericPort(string suffix)
        {
            if (string.IsNullOrEmpty(suffix) || suffix[0] != ':' || suffix.Length == 1)
            {
                return false;
            }

            for (var index = 1; index < suffix.Length; index++)
            {
                if (!char.IsDigit(suffix[index]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
