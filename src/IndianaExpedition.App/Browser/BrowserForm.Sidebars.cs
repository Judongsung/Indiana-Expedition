using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;
using IndianaExpedition.Favorites;

namespace IndianaExpedition.Browser
{
    internal sealed partial class BrowserForm
    {
        private void InitializeExplorerSidebars()
        {
            _explorerSidebars[ExplorerMode.Favorites] = new ExplorerSidebarDefinition(
                () => Strings.FavoritesTitle,
                () => _favoritesSidebarPresenter.Rebuild());
            _explorerSidebars[ExplorerMode.History] = new ExplorerSidebarDefinition(
                () => Strings.HistoryTitle,
                PopulateHistoryTree);
        }

        private void ToggleExplorerSidebar(ExplorerMode mode)
        {
            if (!_explorerSidebars.TryGetValue(mode, out var definition))
            {
                return;
            }

            var resultingMode = _sidebarController.Toggle(mode, !_contentSplit.Panel1Collapsed);
            if (resultingMode == ExplorerMode.None)
            {
                HideExplorerSidebar();
                return;
            }
            ShowExplorerSidebar(resultingMode, _explorerSidebars[resultingMode]);
        }

        private void ShowFavoritesSidebar()
        {
            ShowExplorerSidebar(ExplorerMode.Favorites);
        }

        private void ShowHistorySidebar()
        {
            ShowExplorerSidebar(ExplorerMode.History);
        }

        private void ShowExplorerSidebar(ExplorerMode mode)
        {
            if (_explorerSidebars.TryGetValue(mode, out var definition))
            {
                ShowExplorerSidebar(mode, definition);
            }
        }

        private void ShowExplorerSidebar(ExplorerMode mode, ExplorerSidebarDefinition definition)
        {
            _sidebarController.Show(mode);
            UpdateExplorerButtonStates();
            _explorerTitle.Text = definition.GetTitle();
            _contentSplit.Panel1Collapsed = false;
            if (_contentSplit.Width > 500)
            {
                _contentSplit.SplitterDistance = Math.Min(240, _contentSplit.Width / 3);
            }
            definition.Populate();
        }

        private void HideExplorerSidebar()
        {
            _sidebarController.Hide();
            UpdateExplorerButtonStates();
            _contentSplit.Panel1Collapsed = true;
            _explorerTree.Nodes.Clear();
        }

        private void UpdateExplorerButtonStates()
        {
            foreach (var item in _explorerButtons)
            {
                item.Value.Checked = _sidebarController.IsSelected(item.Key);
            }
            RefreshCommandStates();
        }

        private void PopulateHistoryTree()
        {
            _historySidebarPresenter.Rebuild();
        }

        private void OnExplorerNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs args)
        {
            switch (args.Node.Tag)
            {
                case FavoriteNode favorite when favorite.Kind == FavoriteNodeKind.Link:
                    NavigateTo(favorite.Url, allowExplicitFileUri: true);
                    break;
                case HistoryEntry history:
                    NavigateTo(history.Url);
                    break;
            }
        }

        private void RebuildFavoritesMenu()
        {
            if (_favoritesMenu == null)
            {
                return;
            }

            while (_favoritesMenu.DropDownItems.Count > BrowserUiConstants.FavoriteMenuCommandCount)
            {
                var item = _favoritesMenu.DropDownItems[BrowserUiConstants.FavoriteMenuCommandCount];
                _favoritesMenu.DropDownItems.RemoveAt(BrowserUiConstants.FavoriteMenuCommandCount);
                item.Dispose();
            }

            var favorites = FavoriteProjection.Build(_services.Favorites.Items);
            if (favorites.Count == 0)
            {
                _favoritesMenu.DropDownItems.Add(new ToolStripMenuItem(Strings.Empty) { Enabled = false });
                return;
            }

            foreach (var favorite in favorites)
            {
                _favoritesMenu.DropDownItems.Add(CreateFavoriteMenuItem(favorite));
            }
        }

        private ToolStripMenuItem CreateFavoriteMenuItem(FavoriteProjectionNode favorite)
        {
            var root = CreateFavoriteMenuItemShell(favorite);
            var stack = new Stack<FavoriteMenuBuildItem>();
            AddFavoriteMenuChildren(stack, root, favorite);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var item = CreateFavoriteMenuItemShell(current.Projection);
                current.Parent.DropDownItems.Add(item);
                AddFavoriteMenuChildren(stack, item, current.Projection);
            }
            return root;
        }

        private ToolStripMenuItem CreateFavoriteMenuItemShell(FavoriteProjectionNode favorite)
        {
            var item = new ToolStripMenuItem(favorite.Source.Title)
            {
                Image = favorite.Source.Kind == FavoriteNodeKind.Folder
                    ? Styling.XpGlyphs.Create(Styling.GlyphKind.Folder, 16)
                    : Styling.XpGlyphs.Create(Styling.GlyphKind.Page, 16),
                Tag = favorite.Source
            };

            if (favorite.Source.Kind == FavoriteNodeKind.Folder)
            {
                if (favorite.Children.Count == 0)
                {
                    item.DropDownItems.Add(new ToolStripMenuItem(Strings.Empty) { Enabled = false });
                }
            }
            else
            {
                item.ToolTipText = favorite.Source.Url;
                item.Click += (sender, args) => NavigateTo(favorite.Source.Url, allowExplicitFileUri: true);
            }

            return item;
        }

        private static void AddFavoriteMenuChildren(
            Stack<FavoriteMenuBuildItem> stack,
            ToolStripMenuItem parent,
            FavoriteProjectionNode projection)
        {
            for (var index = projection.Children.Count - 1; index >= 0; index--)
            {
                stack.Push(new FavoriteMenuBuildItem(parent, projection.Children[index]));
            }
        }

        private sealed class FavoriteMenuBuildItem
        {
            internal FavoriteMenuBuildItem(
                ToolStripMenuItem parent,
                FavoriteProjectionNode projection)
            {
                Parent = parent ?? throw new ArgumentNullException(nameof(parent));
                Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            }

            internal ToolStripMenuItem Parent { get; }
            internal FavoriteProjectionNode Projection { get; }
        }
    }
}
