using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private void ShowFavoritesSidebar()
        {
            _explorerMode = ExplorerMode.Favorites;
            _explorerTitle.Text = Strings.FavoritesTitle;
            _contentSplit.Panel1Collapsed = false;
            if (_contentSplit.Width > 500)
            {
                _contentSplit.SplitterDistance = Math.Min(240, _contentSplit.Width / 3);
            }
            PopulateFavoritesTree();
        }

        private void ShowHistorySidebar()
        {
            _explorerMode = ExplorerMode.History;
            _explorerTitle.Text = Strings.HistoryTitle;
            _contentSplit.Panel1Collapsed = false;
            if (_contentSplit.Width > 500)
            {
                _contentSplit.SplitterDistance = Math.Min(240, _contentSplit.Width / 3);
            }
            PopulateHistoryTree();
        }

        private void HideExplorerSidebar()
        {
            _explorerMode = ExplorerMode.None;
            _contentSplit.Panel1Collapsed = true;
            _explorerTree.Nodes.Clear();
        }

        private void PopulateFavoritesTree()
        {
            _explorerTree.BeginUpdate();
            try
            {
                _explorerTree.Nodes.Clear();
                foreach (var item in _services.Favorites.Items)
                {
                    _explorerTree.Nodes.Add(CreateFavoriteTreeNode(item));
                }
            }
            finally
            {
                _explorerTree.EndUpdate();
            }
        }

        private static TreeNode CreateFavoriteTreeNode(FavoriteNode item)
        {
            var imageIndex = item.Kind == FavoriteNodeKind.Folder
                ? BrowserUiConstants.FolderImageIndex
                : BrowserUiConstants.PageImageIndex;
            var node = new TreeNode(item.Title, imageIndex, imageIndex) { Tag = item };
            if (item.Kind == FavoriteNodeKind.Folder)
            {
                foreach (var child in item.Children ?? new List<FavoriteNode>())
                {
                    node.Nodes.Add(CreateFavoriteTreeNode(child));
                }
            }
            return node;
        }

        private void PopulateHistoryTree()
        {
            var now = DateTime.Now.Date;
            var groups = _services.History.Items
                .Select(entry => new { Entry = entry, LocalTime = entry.VisitedAtUtc.ToLocalTime() })
                .GroupBy(item => item.LocalTime.Date)
                .OrderByDescending(group => group.Key);

            _explorerTree.BeginUpdate();
            try
            {
                _explorerTree.Nodes.Clear();
                foreach (var group in groups)
                {
                    var title = FormatHistoryDate(group.Key, now);
                    var dayNode = new TreeNode(
                        title,
                        BrowserUiConstants.HistoryImageIndex,
                        BrowserUiConstants.HistoryImageIndex);
                    foreach (var item in group.OrderByDescending(value => value.LocalTime))
                    {
                        var text = string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.HistoryEntryFormat,
                            item.Entry.Title,
                            item.LocalTime.ToString(BrowserUiConstants.HistoryTimeFormat, CultureInfo.CurrentCulture));
                        dayNode.Nodes.Add(new TreeNode(
                            text,
                            BrowserUiConstants.PageImageIndex,
                            BrowserUiConstants.PageImageIndex) { Tag = item.Entry });
                    }
                    _explorerTree.Nodes.Add(dayNode);
                    dayNode.Expand();
                }
            }
            finally
            {
                _explorerTree.EndUpdate();
            }
        }

        private static string FormatHistoryDate(DateTime date, DateTime today)
        {
            if (date == today)
            {
                return Strings.HistoryToday;
            }
            if (date == today.AddDays(-1))
            {
                return Strings.HistoryYesterday;
            }
            return date.ToString(BrowserUiConstants.HistoryDateFormat, CultureInfo.CurrentCulture);
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

            var favorites = _services.Favorites.Items;
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

        private ToolStripMenuItem CreateFavoriteMenuItem(FavoriteNode favorite)
        {
            var item = new ToolStripMenuItem(favorite.Title)
            {
                Image = favorite.Kind == FavoriteNodeKind.Folder
                    ? Styling.XpGlyphs.Create(Styling.GlyphKind.Folder, 16)
                    : Styling.XpGlyphs.Create(Styling.GlyphKind.Page, 16),
                Tag = favorite
            };

            if (favorite.Kind == FavoriteNodeKind.Folder)
            {
                foreach (var child in favorite.Children ?? new List<FavoriteNode>())
                {
                    item.DropDownItems.Add(CreateFavoriteMenuItem(child));
                }

                if (item.DropDownItems.Count == 0)
                {
                    item.DropDownItems.Add(new ToolStripMenuItem(Strings.Empty) { Enabled = false });
                }
            }
            else
            {
                item.ToolTipText = favorite.Url;
                item.Click += (sender, args) => NavigateTo(favorite.Url, allowExplicitFileUri: true);
            }

            return item;
        }
    }
}
