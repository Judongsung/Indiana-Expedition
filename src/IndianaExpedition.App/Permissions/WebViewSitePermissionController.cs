using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.Permissions
{
    internal sealed class WebViewSitePermissionController : ISitePermissionController
    {
        private readonly CoreWebView2Profile _profile;

        internal WebViewSitePermissionController(CoreWebView2Profile profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public async Task<IReadOnlyList<SitePermissionSetting>> GetSettingsAsync()
        {
            var settings = await _profile.GetNonDefaultPermissionSettingsAsync().ConfigureAwait(true);
            return settings
                .Select(item => new SitePermissionSetting(
                    item.PermissionOrigin,
                    item.PermissionKind,
                    item.PermissionState))
                .OrderBy(item => item.Origin, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => PermissionKindDisplay.GetText(item.Kind), StringComparer.CurrentCulture)
                .ToList();
        }

        public Task SetStateAsync(
            SitePermissionSetting setting,
            CoreWebView2PermissionState state)
        {
            if (setting == null)
            {
                throw new ArgumentNullException(nameof(setting));
            }

            return _profile.SetPermissionStateAsync(setting.Kind, setting.Origin, state);
        }

        public async Task ResetAllAsync()
        {
            var settings = await GetSettingsAsync().ConfigureAwait(true);
            foreach (var setting in settings)
            {
                await SetStateAsync(setting, CoreWebView2PermissionState.Default).ConfigureAwait(true);
            }
        }
    }
}
