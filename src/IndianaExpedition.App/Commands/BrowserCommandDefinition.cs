using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IndianaExpedition.Commands
{
    internal sealed class BrowserCommandDefinition
    {
        internal BrowserCommandDefinition(
            BrowserCommandId id,
            Func<string> getText,
            IEnumerable<Keys> shortcuts,
            Func<bool> canExecute,
            Func<bool> isChecked,
            Func<Task> executeAsync)
        {
            Id = id;
            GetText = getText ?? throw new ArgumentNullException(nameof(getText));
            Shortcuts = new List<Keys>(shortcuts ?? Array.Empty<Keys>());
            CanExecute = canExecute ?? (() => true);
            IsChecked = isChecked ?? (() => false);
            ExecuteAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        }

        internal BrowserCommandId Id { get; }
        internal Func<string> GetText { get; }
        internal IReadOnlyList<Keys> Shortcuts { get; }
        internal Func<bool> CanExecute { get; }
        internal Func<bool> IsChecked { get; }
        internal Func<Task> ExecuteAsync { get; }
    }
}
