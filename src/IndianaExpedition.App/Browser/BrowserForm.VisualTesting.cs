using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Find;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;
using IndianaExpedition.VisualTesting;

namespace IndianaExpedition.Browser
{
    internal sealed partial class BrowserForm
    {
        BrowserApplicationServices IVisualTestSurface.Services => _services;

        Control IVisualTestSurface.ContextMenuOwner => _browserHost;

        void IVisualTestSurface.Reset()
        {
            foreach (var child in _browserHost.Controls.Cast<Control>().ToArray())
            {
                child.Dispose();
            }
            _browserHost.Controls.Clear();
            _addressBox.Text = BrowserDefaults.BlankPageUrl;
            _statusLabel.Text = Strings.Ready;
            _progressBar.Visible = false;
            _stopButton.Enabled = false;
            _backButton.Enabled = false;
            _forwardButton.Enabled = false;
        }

        void IVisualTestSurface.ShowFavorites()
        {
            ShowExplorerSidebar(ExplorerMode.Favorites);
        }

        void IVisualTestSurface.ShowHistory()
        {
            ShowExplorerSidebar(ExplorerMode.History);
        }

        void IVisualTestSurface.ShowBlockedPopup(string sourceOrigin, string targetUrl)
        {
            EnqueueBlockedPopup(sourceOrigin, targetUrl);
        }

        ContextMenuStrip IVisualTestSurface.CreateContextMenu(PageContextMenuModel model)
        {
            return CreatePageContextMenu(model).Menu;
        }

        void IVisualTestSurface.ShowHelpMenu()
        {
            _helpMenu.ShowDropDown();
        }

        string IVisualTestSurface.PrepareDataFile(string fileName)
        {
            var path = Path.Combine(_services.Paths.DataDirectory, fileName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }
            return path;
        }

        void IVisualTestSurface.Present(
            Form captureTarget,
            ContextMenuStrip contextMenu,
            IPageFindController findController,
            System.Drawing.Point? contextMenuLocation)
        {
            _visualTestFindController = findController;
            _visualTestContextMenu = contextMenu;
            if (!ReferenceEquals(captureTarget, this))
            {
                _visualTestDialog = captureTarget;
            }

            PerformLayout();
            Invalidate(true);
            Update();
            _visualTestContextMenu?.Show(
                _browserHost,
                contextMenuLocation ?? System.Drawing.Point.Empty);
            if (!ReferenceEquals(captureTarget, this))
            {
                captureTarget.Show();
                if (captureTarget is LunaForm lunaForm)
                {
                    lunaForm.SendBehindWithoutActivation();
                }
                captureTarget.PerformLayout();
                captureTarget.Invalidate(true);
                captureTarget.Update();
            }
            Application.DoEvents();
            SignalVisualTestReady(captureTarget);
        }

        private void SignalVisualTestReady(Form captureTarget)
        {
            if (string.IsNullOrWhiteSpace(_visualTestReadyFile))
            {
                return;
            }
            var readyFile = Path.GetFullPath(_visualTestReadyFile);
            var directory = Path.GetDirectoryName(readyFile);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(
                readyFile,
                captureTarget.Handle.ToInt64().ToString(CultureInfo.InvariantCulture));
        }
    }
}
