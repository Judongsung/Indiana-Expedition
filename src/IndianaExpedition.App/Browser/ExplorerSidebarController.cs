using System;
using System.Collections.Generic;

namespace IndianaExpedition.Browser
{
    internal enum ExplorerMode
    {
        None,
        Favorites,
        History
    }

    internal sealed class ExplorerSidebarController
    {
        private static readonly IReadOnlyDictionary<ExplorerMode, ExplorerMode> ToggleTargets =
            new Dictionary<ExplorerMode, ExplorerMode>
            {
                [ExplorerMode.Favorites] = ExplorerMode.Favorites,
                [ExplorerMode.History] = ExplorerMode.History
            };

        internal ExplorerMode CurrentMode { get; private set; }

        internal ExplorerMode Toggle(ExplorerMode requestedMode, bool isVisible)
        {
            if (!ToggleTargets.TryGetValue(requestedMode, out var target))
            {
                return CurrentMode;
            }
            CurrentMode = CurrentMode == target && isVisible ? ExplorerMode.None : target;
            return CurrentMode;
        }

        internal void Show(ExplorerMode mode)
        {
            if (!ToggleTargets.ContainsKey(mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }
            CurrentMode = mode;
        }

        internal void Hide()
        {
            CurrentMode = ExplorerMode.None;
        }

        internal bool IsSelected(ExplorerMode mode)
        {
            return CurrentMode == mode;
        }
    }
}
