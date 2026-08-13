using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Permissions
{
    internal static class PermissionKindDisplay
    {
        private static readonly IReadOnlyDictionary<CoreWebView2PermissionKind, Func<string>>
            TextByKind =
                new ReadOnlyDictionary<CoreWebView2PermissionKind, Func<string>>(
                    new Dictionary<CoreWebView2PermissionKind, Func<string>>
                    {
                        [CoreWebView2PermissionKind.UnknownPermission] = () => Strings.PermissionUnknown,
                        [CoreWebView2PermissionKind.Microphone] = () => Strings.PermissionMicrophone,
                        [CoreWebView2PermissionKind.Camera] = () => Strings.PermissionCamera,
                        [CoreWebView2PermissionKind.Geolocation] = () => Strings.PermissionLocation,
                        [CoreWebView2PermissionKind.Notifications] = () => Strings.PermissionNotifications,
                        [CoreWebView2PermissionKind.OtherSensors] = () => Strings.PermissionSensors,
                        [CoreWebView2PermissionKind.ClipboardRead] = () => Strings.PermissionClipboard,
                        [CoreWebView2PermissionKind.MultipleAutomaticDownloads] = () => Strings.PermissionMultipleDownloads,
                        [CoreWebView2PermissionKind.FileReadWrite] = () => Strings.PermissionFileAccess,
                        [CoreWebView2PermissionKind.Autoplay] = () => Strings.PermissionAutoplay,
                        [CoreWebView2PermissionKind.LocalFonts] = () => Strings.PermissionLocalFonts,
                        [CoreWebView2PermissionKind.MidiSystemExclusiveMessages] = () => Strings.PermissionMidi,
                        [CoreWebView2PermissionKind.WindowManagement] = () => Strings.PermissionWindowManagement,
                        [CoreWebView2PermissionKind.PersistentStorage] = () => Strings.PermissionPersistentStorage
                    });

        internal static string GetText(CoreWebView2PermissionKind kind)
        {
            return TextByKind.TryGetValue(kind, out var getText)
                ? getText()
                : Strings.PermissionUnknown;
        }
    }

    internal static class PermissionStateDisplay
    {
        private static readonly IReadOnlyDictionary<CoreWebView2PermissionState, Func<string>>
            TextByState =
                new ReadOnlyDictionary<CoreWebView2PermissionState, Func<string>>(
                    new Dictionary<CoreWebView2PermissionState, Func<string>>
                    {
                        [CoreWebView2PermissionState.Allow] = () => Strings.PermissionAllowed,
                        [CoreWebView2PermissionState.Deny] = () => Strings.PermissionBlocked,
                        [CoreWebView2PermissionState.Default] = () => Strings.PermissionDefault
                    });

        internal static string GetText(CoreWebView2PermissionState state)
        {
            return TextByState.TryGetValue(state, out var getText)
                ? getText()
                : Strings.PermissionDefault;
        }
    }
}
