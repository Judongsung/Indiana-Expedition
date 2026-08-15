using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;

namespace IndianaExpedition.Commands
{
    internal sealed class BrowserCommandCatalog
    {
        private readonly IReadOnlyDictionary<BrowserCommandId, BrowserCommandDefinition> _definitions;
        private readonly IReadOnlyDictionary<Keys, BrowserCommandId> _shortcuts;

        internal BrowserCommandCatalog(IEnumerable<BrowserCommandDefinition> definitions)
        {
            var definitionMap = (definitions ?? throw new ArgumentNullException(nameof(definitions)))
                .ToDictionary(item => item.Id);
            var shortcutMap = new Dictionary<Keys, BrowserCommandId>();
            foreach (var definition in definitionMap.Values)
            {
                foreach (var shortcut in definition.Shortcuts.Where(value => value != Keys.None))
                {
                    shortcutMap.Add(shortcut, definition.Id);
                }
            }
            _definitions = new ReadOnlyDictionary<BrowserCommandId, BrowserCommandDefinition>(definitionMap);
            _shortcuts = new ReadOnlyDictionary<Keys, BrowserCommandId>(shortcutMap);
        }

        internal IEnumerable<BrowserCommandDefinition> Definitions => _definitions.Values;

        internal BrowserCommandDefinition Get(BrowserCommandId id)
        {
            return _definitions[id];
        }

        internal bool TryResolveShortcut(Keys shortcut, out BrowserCommandId id)
        {
            return _shortcuts.TryGetValue(shortcut, out id);
        }
    }
}
