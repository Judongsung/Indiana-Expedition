using System;
using System.Drawing;
using System.Windows.Forms;

namespace IndianaExpedition.ContextMenus
{
    internal sealed class WebViewContextMenuSession : IDisposable
    {
        private readonly PageContextMenuDefinition _definition;
        private readonly ContextMenuStrip _menu;
        private readonly IContextMenuDeferral _deferral;
        private readonly IUiCommandDispatcher _dispatcher;
        private readonly Control _owner;
        private Action _selectedCommand;
        private bool _disposed;

        internal WebViewContextMenuSession(
            PageContextMenuDefinition definition,
            IContextMenuDeferral deferral,
            IUiCommandDispatcher dispatcher,
            Control owner)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _menu = definition.Menu;
            _deferral = deferral ?? throw new ArgumentNullException(nameof(deferral));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _menu.ItemClicked += OnItemClicked;
            _menu.Closed += OnMenuClosed;
        }

        internal event EventHandler Closed;

        internal void Show(Point location)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebViewContextMenuSession));
            }

            _menu.Show(_owner, location);
        }

        public void Dispose()
        {
            CompleteSession(command: null);
        }

        internal void Close()
        {
            CompleteSession(_selectedCommand);
        }

        private void OnItemClicked(object sender, ToolStripItemClickedEventArgs args)
        {
            _selectedCommand = null;
            if (args?.ClickedItem?.Enabled == true)
            {
                _definition.TryGetCommand(args.ClickedItem, out _selectedCommand);
            }
        }

        private void OnMenuClosed(object sender, ToolStripDropDownClosedEventArgs args)
        {
            Close();
        }

        private void CompleteSession(Action command)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _menu.ItemClicked -= OnItemClicked;
            _menu.Closed -= OnMenuClosed;
            var deferralCompleted = false;
            try
            {
                _deferral.Complete();
                deferralCompleted = true;
            }
            finally
            {
                try
                {
                    Closed?.Invoke(this, EventArgs.Empty);
                }
                finally
                {
                    DispatchAfterContextMenuRequest(
                        _owner,
                        _menu,
                        deferralCompleted ? command : null);
                }
            }
        }

        private void DispatchAfterContextMenuRequest(
            Control owner,
            ContextMenuStrip menu,
            Action command)
        {
            if (owner == null || owner.IsDisposed || !owner.IsHandleCreated)
            {
                menu.Dispose();
                return;
            }

            var posted = _dispatcher.TryPost(owner, () =>
            {
                try
                {
                    menu.Dispose();
                }
                finally
                {
                    command?.Invoke();
                }
            });
            if (!posted)
            {
                menu.Dispose();
            }
        }
    }
}
