using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Find;
using IndianaExpedition.Resources;
using IndianaExpedition.Permissions;

namespace IndianaExpedition.Browser
{
    internal sealed partial class BrowserForm
    {
        private void AttachWebViewFeatures(WebView2 webView)
        {
            _pageFindController?.Dispose();
            _pageFindController = new WebViewPageFindController(webView.CoreWebView2);
            _sitePermissionController = new WebViewSitePermissionController(webView.CoreWebView2.Profile);
            ApplyZoomSetting(_services.Settings.Current.DefaultZoomLevel);
        }

        private void DetachWebViewFeatures(WebView2 webView)
        {
            _pageFindController?.Dispose();
            _pageFindController = null;
            _sitePermissionController = null;
        }

        private void ShowPageFindDialog()
        {
            if (_pageFindController == null)
            {
                MessageBox.Show(Strings.FindUnavailable, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new PageFindDialog(_pageFindController, _lastFindCriteria))
            {
                dialog.ShowDialog(this);
                _lastFindCriteria = dialog.Criteria.Clone();
            }
        }

        private async Task RepeatPageFindAsync(bool previous)
        {
            if (_pageFindController == null || string.IsNullOrWhiteSpace(_lastFindCriteria.Term))
            {
                ShowPageFindDialog();
                return;
            }
            await _pageFindController.RepeatAsync(previous).ConfigureAwait(true);
        }

        private void PrintPage()
        {
            if (CoreWebView == null)
            {
                MessageBox.Show(Strings.PrintUnavailable, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                CoreWebView.ShowPrintUI(CoreWebView2PrintDialogKind.System);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetZoomLevel(BrowserZoomLevel level)
        {
            _services.Settings.Update(settings => settings.DefaultZoomLevel = level);
        }

        private void StepZoomLevel(int direction)
        {
            var level = BrowserZoomCatalog.Step(_services.Settings.Current.DefaultZoomLevel, direction);
            SetZoomLevel(level);
        }

        private void ApplyZoomSetting(BrowserZoomLevel level)
        {
            var definition = BrowserZoomCatalog.Get(level);
            var normalizedLevel = definition.Level;
            foreach (var item in _zoomMenuItems)
            {
                item.Value.Checked = item.Key == normalizedLevel;
            }

            if (_zoomLabel != null)
            {
                _zoomLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ZoomStatusFormat,
                    definition.Percentage);
            }

            if (_webView?.CoreWebView2 == null)
            {
                return;
            }

            var factor = definition.Factor;
            if (Math.Abs(_webView.ZoomFactor - factor) > BrowserZoomCatalog.FactorComparisonTolerance)
            {
                _webView.ZoomFactor = factor;
            }
        }

    }
}
