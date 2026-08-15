using System;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Popups;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Browser
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
            _popupBlockerPresenter.Enqueue(sourceOrigin, targetUri);
            _statusLabel.Text = Strings.PopupBlockedStatus;
        }

        private void OpenOldestBlockedPopup()
        {
            _popupBlockerPresenter.OpenOldest();
        }

        private void AllowOldestPopupOrigin()
        {
            if (_popupBlockerPresenter.TryAllowOldestOrigin(out var maximumOrigins))
            {
                return;
            }
            MessageBox.Show(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.PopupOriginLimitFormat,
                    maximumOrigins),
                Branding.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void DismissBlockedPopups()
        {
            _popupBlockerPresenter.Dismiss();
        }

        private void OnPopupBlockerStateChanged(
            object sender,
            PopupBlockerStateChangedEventArgs state)
        {
            var visible = state.Count > 0;
            _informationBarLabel.Text = visible
                ? string.Format(CultureInfo.CurrentCulture, Strings.PopupBlockedFormat, state.Count)
                : string.Empty;
            _openBlockedPopupButton.Enabled = state.CanOpen;
            _allowPopupOriginButton.Enabled = state.CanAllowOrigin;
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

        private void ShowPopupBlockerSettingsDialog()
        {
            using (var dialog = new PopupBlockerSettingsDialog(_services.Settings))
            {
                dialog.ShowDialog(this);
            }
        }
    }
}
