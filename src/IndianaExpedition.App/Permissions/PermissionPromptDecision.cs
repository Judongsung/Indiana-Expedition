using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.Permissions
{
    internal enum PermissionPromptDecision
    {
        AllowOnce,
        AlwaysAllow,
        BlockOnce,
        AlwaysBlock
    }

    internal sealed class PermissionPromptResponse
    {
        internal PermissionPromptResponse(
            CoreWebView2PermissionState state,
            bool saveInProfile)
        {
            State = state;
            SaveInProfile = saveInProfile;
        }

        internal CoreWebView2PermissionState State { get; }

        internal bool SaveInProfile { get; }
    }
}
