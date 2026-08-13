using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Dialogs;
using IndianaExpedition.Resources;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private bool ShouldAllowPopup(CoreWebView2NewWindowRequestedEventArgs args, out string sourceOrigin)
        {
            var source = GetPopupSource(args);
            PopupPolicy.TryNormalizeOrigin(source, out sourceOrigin);
            var settings = _services.Settings.Current;
            return PopupPolicy.ShouldAllow(
                args.IsUserInitiated,
                settings.PopupBlockerEnabled,
                source,
                settings.AllowedPopupOrigins);
        }

        private string GetPopupSource(CoreWebView2NewWindowRequestedEventArgs args)
        {
            try
            {
                var frameSource = args.OriginalSourceFrameInfo?.Source;
                if (PopupPolicy.TryNormalizeOrigin(frameSource, out _))
                {
                    return frameSource;
                }
            }
            catch (NotImplementedException)
            {
                // Older compatible runtimes may not expose frame information.
            }

            return CoreWebView?.Source;
        }

        private void EnqueueBlockedPopup(string sourceOrigin, string targetUri)
        {
            while (_blockedPopups.Count >= PopupUiConstants.MaximumPendingPopups)
            {
                _blockedPopups.Dequeue();
            }

            _blockedPopups.Enqueue(new BlockedPopupRequest(sourceOrigin, targetUri));
            RefreshPopupInformationBar();
            _statusLabel.Text = Strings.PopupBlockedStatus;
        }

        private void OpenOldestBlockedPopup()
        {
            while (_blockedPopups.Count > 0)
            {
                var request = _blockedPopups.Dequeue();
                if (IsOpenablePopupTarget(request.TargetUri))
                {
                    _application.OpenWindow(request.TargetUri);
                    break;
                }
            }

            RefreshPopupInformationBar();
        }

        private void AllowOldestPopupOrigin()
        {
            var origin = _blockedPopups
                .Select(request => request.SourceOrigin)
                .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
            if (string.IsNullOrWhiteSpace(origin))
            {
                return;
            }

            var allowedOrigins = _services.Settings.Current.AllowedPopupOrigins;
            if (!allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase) &&
                allowedOrigins.Count >= PopupPolicyConstants.MaximumAllowedOrigins)
            {
                MessageBox.Show(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.PopupOriginLimitFormat,
                        PopupPolicyConstants.MaximumAllowedOrigins),
                    Branding.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _services.Settings.Update(settings =>
            {
                settings.AllowedPopupOrigins.Add(origin);
            });

            var remaining = _blockedPopups
                .Where(request => !string.Equals(request.SourceOrigin, origin, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            _blockedPopups.Clear();
            foreach (var request in remaining)
            {
                _blockedPopups.Enqueue(request);
            }

            RefreshPopupInformationBar();
        }

        private void DismissBlockedPopups()
        {
            _blockedPopups.Clear();
            RefreshPopupInformationBar();
        }

        private void RefreshPopupInformationBar()
        {
            var visible = _blockedPopups.Count > 0;
            _informationBarLabel.Text = visible
                ? string.Format(CultureInfo.CurrentCulture, Strings.PopupBlockedFormat, _blockedPopups.Count)
                : string.Empty;
            _openBlockedPopupButton.Enabled = _blockedPopups.Any(request => IsOpenablePopupTarget(request.TargetUri));
            _allowPopupOriginButton.Enabled = _blockedPopups.Any(request => !string.IsNullOrWhiteSpace(request.SourceOrigin));
            SetInformationBarVisible(visible);
        }

        private void SetInformationBarVisible(bool visible)
        {
            var displayed = visible && !_fullScreen;
            _informationBar.Visible = displayed;
            _rootLayout.RowStyles[BrowserLayoutConstants.InformationBarRow].Height = displayed
                ? BrowserLayoutConstants.InformationBarHeight
                : 0f;
        }

        private void OnPopupBlockerEnabledClicked(object sender, EventArgs args)
        {
            var enabled = _popupBlockerEnabledMenuItem.Checked;
            _services.Settings.Update(settings => settings.PopupBlockerEnabled = enabled);
            if (!enabled)
            {
                DismissBlockedPopups();
            }
        }

        private void ShowPopupBlockerSettingsDialog()
        {
            using (var dialog = new PopupBlockerSettingsDialog(_services.Settings))
            {
                dialog.ShowDialog(this);
            }
        }

        private static bool IsOpenablePopupTarget(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private sealed class BlockedPopupRequest
        {
            internal BlockedPopupRequest(string sourceOrigin, string targetUri)
            {
                SourceOrigin = sourceOrigin;
                TargetUri = targetUri;
            }

            internal string SourceOrigin { get; }

            internal string TargetUri { get; }
        }
    }
}
