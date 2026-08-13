using System;
using System.Globalization;
using System.Windows.Forms;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private ContextMenuStrip CreatePageContextMenu(PageContextMenuModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var menu = new ContextMenuStrip { Renderer = new XpToolStripRenderer() };
            var back = menu.Items.Add(Strings.ContextBack, null, (sender, args) => GoBack());
            back.Enabled = CoreWebView?.CanGoBack == true;
            var forward = menu.Items.Add(Strings.ContextForward, null, (sender, args) => GoForward());
            forward.Enabled = CoreWebView?.CanGoForward == true;
            menu.Items.Add(Strings.ContextRefresh, null, (sender, args) => RefreshPage());
            menu.Items.Add(new ToolStripSeparator());

            if (model.HasLink)
            {
                menu.Items.Add(
                    Strings.ContextOpenLinkNewWindow,
                    null,
                    (sender, args) => _application.OpenWindow(model.LinkUri));
                menu.Items.Add(
                    Strings.ContextCopyShortcut,
                    null,
                    (sender, args) => Clipboard.SetText(model.LinkUri));
                menu.Items.Add(new ToolStripSeparator());
            }

            var copy = menu.Items.Add(Strings.ContextCopy, null, (sender, args) =>
                Clipboard.SetText(model.SelectionText));
            copy.Enabled = model.HasSelection;
            menu.Items.Add(Strings.ContextSelectAll, null, (sender, args) =>
                _ = CoreWebView?.ExecuteScriptAsync(string.Format(
                    CultureInfo.InvariantCulture,
                    BrowserScriptConstants.ExecuteCommandTemplate,
                    BrowserScriptConstants.SelectAllCommand)));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(Strings.ContextProperties) { Enabled = false });
            return menu;
        }

        private void ReplaceContextMenuSession(WebViewContextMenuSession session)
        {
            _contextMenuSession?.Dispose();
            _contextMenuSession = session;
            if (session != null)
            {
                session.Closed += OnContextMenuSessionClosed;
            }
        }

        private void OnContextMenuSessionClosed(object sender, EventArgs args)
        {
            if (sender is WebViewContextMenuSession session)
            {
                session.Closed -= OnContextMenuSessionClosed;
                if (ReferenceEquals(_contextMenuSession, session))
                {
                    _contextMenuSession = null;
                }
            }
        }
    }
}
