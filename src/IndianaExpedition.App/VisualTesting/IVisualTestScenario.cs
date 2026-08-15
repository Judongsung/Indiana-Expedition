using System;
using System.Windows.Forms;
using System.Drawing;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Find;

namespace IndianaExpedition.VisualTesting
{
    internal interface IVisualTestScenario
    {
        void Prepare(IVisualTestSurface surface);
    }

    internal interface IVisualTestSurface
    {
        BrowserApplicationServices Services { get; }
        Control ContextMenuOwner { get; }
        void Reset();
        void ShowFavorites();
        void ShowHistory();
        void ShowBlockedPopup(string sourceOrigin, string targetUrl);
        ContextMenuStrip CreateContextMenu(PageContextMenuModel model);
        void ShowHelpMenu();
        string PrepareDataFile(string fileName);
        void Present(
            Form captureTarget,
            ContextMenuStrip contextMenu = null,
            IPageFindController findController = null,
            Point? contextMenuLocation = null);
    }
}
