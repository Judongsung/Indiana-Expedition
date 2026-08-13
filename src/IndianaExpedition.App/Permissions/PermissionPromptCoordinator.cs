using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.Permissions
{
    internal sealed class PermissionPromptCoordinator
    {
        private static readonly IReadOnlyDictionary<CoreWebView2PermissionKind, PermissionPromptResponse>
            AutomaticResponses =
                new ReadOnlyDictionary<CoreWebView2PermissionKind, PermissionPromptResponse>(
                    new Dictionary<CoreWebView2PermissionKind, PermissionPromptResponse>
                    {
                        [CoreWebView2PermissionKind.UnknownPermission] = new PermissionPromptResponse(
                            CoreWebView2PermissionState.Deny,
                            saveInProfile: false)
                    });

        internal void Handle(
            IWin32Window owner,
            CoreWebView2PermissionRequestedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            args.Handled = true;
            var response = GetResponse(owner, args.Uri, args.PermissionKind);
            args.State = response.State;
            args.SavesInProfile = response.SaveInProfile;
        }

        private static PermissionPromptResponse GetResponse(
            IWin32Window owner,
            string origin,
            CoreWebView2PermissionKind kind)
        {
            if (AutomaticResponses.TryGetValue(kind, out var automaticResponse))
            {
                return automaticResponse;
            }

            using (var dialog = new PermissionRequestDialog(origin, kind))
            {
                dialog.ShowDialog(owner);
                return dialog.Response;
            }
        }
    }
}
