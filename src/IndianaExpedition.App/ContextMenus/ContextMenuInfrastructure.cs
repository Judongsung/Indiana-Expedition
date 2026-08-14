using System;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.ContextMenus
{
    internal interface IContextMenuDeferral
    {
        void Complete();
    }

    internal sealed class CoreWebViewContextMenuDeferral : IContextMenuDeferral
    {
        private readonly CoreWebView2Deferral _deferral;

        internal CoreWebViewContextMenuDeferral(CoreWebView2Deferral deferral)
        {
            _deferral = deferral ?? throw new ArgumentNullException(nameof(deferral));
        }

        public void Complete()
        {
            _deferral.Complete();
        }
    }

    internal interface IUiCommandDispatcher
    {
        bool TryPost(Control owner, Action action);
    }

    internal sealed class WinFormsUiCommandDispatcher : IUiCommandDispatcher
    {
        internal static readonly WinFormsUiCommandDispatcher Instance =
            new WinFormsUiCommandDispatcher();

        private WinFormsUiCommandDispatcher()
        {
        }

        public bool TryPost(Control owner, Action action)
        {
            if (owner == null || owner.IsDisposed || !owner.IsHandleCreated || action == null)
            {
                return false;
            }

            try
            {
                owner.BeginInvoke(new MethodInvoker(action));
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
