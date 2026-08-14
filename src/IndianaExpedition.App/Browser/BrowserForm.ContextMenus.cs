using System;
using System.Globalization;
using System.Windows.Forms;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Constants;

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

            var commands = new PageContextMenuCommandMap();
            commands.Add(PageContextMenuCommand.Back, GoBack, CoreWebView?.CanGoBack == true);
            commands.Add(PageContextMenuCommand.Forward, GoForward, CoreWebView?.CanGoForward == true);
            commands.Add(PageContextMenuCommand.Refresh, RefreshPage);
            commands.Add(
                PageContextMenuCommand.OpenLinkNewWindow,
                () => _application.OpenWindow(model.LinkUri));
            commands.Add(PageContextMenuCommand.CopyLink, () => Clipboard.SetText(model.LinkUri));
            commands.Add(
                PageContextMenuCommand.CopySelection,
                () => Clipboard.SetText(model.SelectionText));
            commands.Add(PageContextMenuCommand.SelectAll, () =>
                _ = CoreWebView?.ExecuteScriptAsync(string.Format(
                    CultureInfo.InvariantCulture,
                    BrowserScriptConstants.ExecuteCommandTemplate,
                    BrowserScriptConstants.SelectAllCommand)));
            return PageContextMenuFactory.Create(model, commands);
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
