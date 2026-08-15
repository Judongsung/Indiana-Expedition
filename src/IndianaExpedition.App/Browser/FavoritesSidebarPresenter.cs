using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Favorites;

namespace IndianaExpedition.Browser
{
    internal sealed class FavoritesSidebarPresenter
    {
        private readonly TreeView _tree;
        private readonly Func<IReadOnlyList<FavoriteNode>> _getItems;

        internal FavoritesSidebarPresenter(
            TreeView tree,
            Func<IReadOnlyList<FavoriteNode>> getItems)
        {
            _tree = tree ?? throw new ArgumentNullException(nameof(tree));
            _getItems = getItems ?? throw new ArgumentNullException(nameof(getItems));
        }

        internal void Rebuild()
        {
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                _tree.Nodes.AddRange(
                    FavoriteTreeNodeFactory.Build(FavoriteProjection.Build(_getItems())).ToArray());
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

    }
}
