using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.Permissions
{
    internal sealed class SitePermissionSetting
    {
        internal SitePermissionSetting(
            string origin,
            CoreWebView2PermissionKind kind,
            CoreWebView2PermissionState state)
        {
            Origin = origin;
            Kind = kind;
            State = state;
        }

        internal string Origin { get; }

        internal CoreWebView2PermissionKind Kind { get; }

        internal CoreWebView2PermissionState State { get; }
    }

    internal interface ISitePermissionController
    {
        Task<IReadOnlyList<SitePermissionSetting>> GetSettingsAsync();

        Task SetStateAsync(
            SitePermissionSetting setting,
            CoreWebView2PermissionState state);

        Task ResetAllAsync();
    }
}
