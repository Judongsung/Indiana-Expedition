using System;
using System.Globalization;
using System.Windows.Forms;
using IndianaExpedition.ContextMenus;
using IndianaExpedition.Constants;
using IndianaExpedition.Commands;

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
            commands.Add(
                PageContextMenuCommand.Back,
                () => _commandRouter.Execute(BrowserCommandId.Back),
                _commandCatalog.Get(BrowserCommandId.Back).CanExecute());
            commands.Add(
                PageContextMenuCommand.Forward,
                () => _commandRouter.Execute(BrowserCommandId.Forward),
                _commandCatalog.Get(BrowserCommandId.Forward).CanExecute());
            commands.Add(
                PageContextMenuCommand.Refresh,
                () => _commandRouter.Execute(BrowserCommandId.Refresh));
            commands.Add(
                PageContextMenuCommand.OpenLinkNewWindow,
                () => _application.OpenWindow(model.LinkUri));
            commands.Add(PageContextMenuCommand.CopyLink, () => _clipboardService.SetText(model.LinkUri));
            commands.Add(
                PageContextMenuCommand.CopySelection,
                () => _clipboardService.SetText(model.SelectionText));
            commands.Add(
                PageContextMenuCommand.SelectAll,
                () => _commandRouter.Execute(BrowserCommandId.SelectAll));
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
