using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IndianaExpedition.Commands
{
    internal sealed class BrowserCommandRouter
    {
        private readonly BrowserCommandCatalog _catalog;
        private readonly IUiCommandExecutor _executor;

        internal BrowserCommandRouter(BrowserCommandCatalog catalog, IUiCommandExecutor executor)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        internal bool TryExecuteShortcut(Keys keys)
        {
            if (!_catalog.TryResolveShortcut(keys, out var command))
            {
                return false;
            }
            Execute(command);
            return true;
        }

        internal void Execute(BrowserCommandId id)
        {
            var definition = _catalog.Get(id);
            if (!definition.CanExecute())
            {
                return;
            }
            _executor.Execute(definition.ExecuteAsync);
        }
    }

    internal interface IUiCommandExecutor
    {
        void Execute(Func<Task> command);
    }

    internal sealed class UiCommandExecutor : IUiCommandExecutor
    {
        private readonly Action<Exception> _onError;

        internal UiCommandExecutor(Action<Exception> onError)
        {
            _onError = onError ?? throw new ArgumentNullException(nameof(onError));
        }

        public async void Execute(Func<Task> command)
        {
            try
            {
                await command().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _onError(ex);
            }
        }
    }
}
