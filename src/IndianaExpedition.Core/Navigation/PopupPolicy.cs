using System;
using System.Collections.Generic;
using System.Linq;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Navigation
{
    internal static class PopupPolicy
    {
        internal static bool ShouldAllow(
            bool isUserInitiated,
            bool blockerEnabled,
            string source,
            IEnumerable<string> allowedOrigins)
        {
            if (isUserInitiated || !blockerEnabled)
            {
                return true;
            }

            return TryNormalizeOrigin(source, out var origin) &&
                   NormalizeOrigins(allowedOrigins).Contains(origin, StringComparer.OrdinalIgnoreCase);
        }

        internal static bool TryNormalizeOrigin(string value, out string origin)
        {
            origin = null;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(uri.Host))
            {
                return false;
            }

            try
            {
                var builder = new UriBuilder(
                    uri.Scheme.ToLowerInvariant(),
                    uri.IdnHost.ToLowerInvariant(),
                    uri.IsDefaultPort ? -1 : uri.Port);
                origin = builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                return !string.IsNullOrWhiteSpace(origin);
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        internal static List<string> NormalizeOrigins(IEnumerable<string> origins)
        {
            var normalized = new List<string>();
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in origins ?? Enumerable.Empty<string>())
            {
                if (!TryNormalizeOrigin(candidate, out var origin) || !unique.Add(origin))
                {
                    continue;
                }

                normalized.Add(origin);
                if (normalized.Count >= PopupPolicyConstants.MaximumAllowedOrigins)
                {
                    break;
                }
            }

            return normalized;
        }
    }
}
