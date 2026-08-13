using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Resources;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private void AttachWebViewFeatures(WebView2 webView)
        {
            _pageFindController?.Dispose();
            _pageFindController = new WebViewPageFindController(webView.CoreWebView2);
            ApplyZoomSetting(_services.Settings.Current.DefaultZoomLevel);
        }

        private void DetachWebViewFeatures(WebView2 webView)
        {
            _pageFindController?.Dispose();
            _pageFindController = null;
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

        private async void RepeatPageFind(bool previous)
        {
            if (_pageFindController == null || string.IsNullOrWhiteSpace(_lastFindCriteria.Term))
            {
                ShowPageFindDialog();
                return;
            }

            try
            {
                await _pageFindController.RepeatAsync(previous).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            var level = BrowserZoomPolicy.Step(_services.Settings.Current.DefaultZoomLevel, direction);
            SetZoomLevel(level);
        }

        private void ApplyZoomSetting(BrowserZoomLevel level)
        {
            var normalizedLevel = BrowserZoomPolicy.Normalize(level);
            foreach (var item in _zoomMenuItems)
            {
                item.Value.Checked = item.Key == normalizedLevel;
            }

            if (_zoomLabel != null)
            {
                _zoomLabel.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.ZoomStatusFormat,
                    BrowserZoomConstants.GetPercentage(normalizedLevel));
            }

            if (_webView?.CoreWebView2 == null)
            {
                return;
            }

            var factor = BrowserZoomConstants.GetFactor(normalizedLevel);
            if (Math.Abs(_webView.ZoomFactor - factor) > BrowserZoomConstants.FactorComparisonTolerance)
            {
                _webView.ZoomFactor = factor;
            }
        }

        private static ManagedBrowserCommand ResolveManagedBrowserShortcut(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.F:
                    return ManagedBrowserCommand.Find;
                case Keys.F3:
                    return ManagedBrowserCommand.FindNext;
                case Keys.Shift | Keys.F3:
                    return ManagedBrowserCommand.FindPrevious;
                case Keys.Control | Keys.P:
                    return ManagedBrowserCommand.Print;
                case Keys.Control | Keys.Add:
                case Keys.Control | Keys.Oemplus:
                case Keys.Control | Keys.Shift | Keys.Oemplus:
                    return ManagedBrowserCommand.ZoomIn;
                case Keys.Control | Keys.Subtract:
                case Keys.Control | Keys.OemMinus:
                    return ManagedBrowserCommand.ZoomOut;
                case Keys.Control | Keys.D0:
                case Keys.Control | Keys.NumPad0:
                    return ManagedBrowserCommand.ZoomReset;
                default:
                    return ManagedBrowserCommand.None;
            }
        }

        private void ExecuteManagedBrowserCommand(ManagedBrowserCommand command)
        {
            switch (command)
            {
                case ManagedBrowserCommand.Find:
                    ShowPageFindDialog();
                    break;
                case ManagedBrowserCommand.FindNext:
                    RepeatPageFind(previous: false);
                    break;
                case ManagedBrowserCommand.FindPrevious:
                    RepeatPageFind(previous: true);
                    break;
                case ManagedBrowserCommand.Print:
                    PrintPage();
                    break;
                case ManagedBrowserCommand.ZoomIn:
                    StepZoomLevel(1);
                    break;
                case ManagedBrowserCommand.ZoomOut:
                    StepZoomLevel(-1);
                    break;
                case ManagedBrowserCommand.ZoomReset:
                    SetZoomLevel(BrowserZoomLevel.Medium);
                    break;
            }
        }

        private enum ManagedBrowserCommand
        {
            None,
            Find,
            FindNext,
            FindPrevious,
            Print,
            ZoomIn,
            ZoomOut,
            ZoomReset
        }
    }
}
