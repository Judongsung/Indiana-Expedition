using System;
using System.Globalization;
using System.Windows.Forms;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Browser
{
    internal sealed partial class BrowserForm
    {
        private PageContextMenuDefinition CreatePageContextMenu(PageContextMenuModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var definition = new PageContextMenuDefinition(
                new ContextMenuStrip { Renderer = new XpToolStripRenderer() });
            var back = definition.AddCommand(Strings.ContextBack, GoBack);
            back.Enabled = CoreWebView?.CanGoBack == true;
            var forward = definition.AddCommand(Strings.ContextForward, GoForward);
            forward.Enabled = CoreWebView?.CanGoForward == true;
            definition.AddCommand(Strings.ContextRefresh, RefreshPage);
            definition.AddSeparator();

            if (model.HasLink)
            {
                definition.AddCommand(
                    Strings.ContextOpenLinkNewWindow,
                    () => _application.OpenWindow(model.LinkUri));
                definition.AddCommand(
                    Strings.ContextCopyShortcut,
                    () => Clipboard.SetText(model.LinkUri));
                definition.AddSeparator();
            }

            var copy = definition.AddCommand(
                Strings.ContextCopy,
                () => Clipboard.SetText(model.SelectionText));
            copy.Enabled = model.HasSelection;
            definition.AddCommand(Strings.ContextSelectAll, () =>
                _ = CoreWebView?.ExecuteScriptAsync(string.Format(
                    CultureInfo.InvariantCulture,
                    BrowserScriptConstants.ExecuteCommandTemplate,
                    BrowserScriptConstants.SelectAllCommand)));
            definition.AddSeparator();
            definition.AddDisabledItem(Strings.ContextProperties);
            return definition;
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
