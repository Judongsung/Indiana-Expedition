using System;
using System.Drawing;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.ContextMenus
{
    internal sealed class WebViewContextMenuRequestSnapshot
    {
        private WebViewContextMenuRequestSnapshot(
            PageContextMenuModel model,
            Point location,
            IContextMenuDeferral deferral)
        {
            Model = model;
            Location = location;
            Deferral = deferral;
        }

        internal PageContextMenuModel Model { get; }
        internal Point Location { get; }
        internal IContextMenuDeferral Deferral { get; }

        internal static WebViewContextMenuRequestSnapshot Create(
            CoreWebView2ContextMenuRequestedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }
            var target = args.ContextMenuTarget;
            var model = new PageContextMenuModel(
                target?.HasLinkUri == true ? target.LinkUri : null,
                target?.HasSelection == true ? target.SelectionText : null);
            return new WebViewContextMenuRequestSnapshot(
                model,
                args.Location,
                new CoreWebViewContextMenuDeferral(args.GetDeferral()));
        }
    }
}
