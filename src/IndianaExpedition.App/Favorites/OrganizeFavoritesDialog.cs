using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Favorites
{
    internal sealed class OrganizeFavoritesDialog : LunaForm
    {
        private readonly FavoritesService _favorites;
        private readonly TreeView _tree;
        private readonly Button _renameButton;
        private readonly Button _moveButton;
        private readonly Button _deleteButton;

        internal OrganizeFavoritesDialog(FavoritesService favorites)
        {
            _favorites = favorites;
            Text = Strings.OrganizeFavoritesTitle;
            SetContentClientSize(620, 420);
            MinimumSize = new Size(520, 340);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            var images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            images.Images.Add(XpGlyphs.Create(GlyphKind.Folder, 16));
            images.Images.Add(XpGlyphs.Create(GlyphKind.Page, 16));

            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                ImageList = images,
                HideSelection = false,
                BorderStyle = BorderStyle.Fixed3D
            };
            _tree.AfterSelect += (sender, args) => UpdateButtonState();

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 126,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10),
                WrapContents = false
            };
            var newFolder = CreateActionButton(Strings.NewFolderButton, OnNewFolder);
            _renameButton = CreateActionButton(Strings.RenameButton, OnRename);
            _moveButton = CreateActionButton(Strings.MoveToFolderButton, OnMove);
            _deleteButton = CreateActionButton(Strings.DeleteButton, OnDelete);
            buttonPanel.Controls.AddRange(new Control[] { newFolder, _renameButton, _moveButton, _deleteButton });

            var close = new XpButton
            {
                Text = Strings.CloseButton,
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(518, 382),
                Size = new Size(82, 27)
            };

            var treePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = XpPalette.ControlFace,
                Padding = new Padding(12, 12, 0, 48)
            };
            treePanel.Controls.Add(_tree);
            ContentPanel.Controls.Add(treePanel);
            ContentPanel.Controls.Add(buttonPanel);
            ContentPanel.Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;

            Resize += (sender, args) => close.Location = new Point(
                ContentPanel.ClientSize.Width - 102,
                ContentPanel.ClientSize.Height - 38);
            ReloadTree();
        }

        private Button CreateActionButton(string text, EventHandler click)
        {
            var button = new XpButton { Text = text, Width = 104, Height = 29, Margin = new Padding(0, 0, 0, 8) };
            button.Click += click;
            return button;
        }

        private void ReloadTree(Guid? selectId = null)
        {
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                foreach (var favorite in _favorites.Items)
                {
                    _tree.Nodes.Add(CreateNode(favorite));
                }
                _tree.ExpandAll();

                if (selectId.HasValue)
                {
                    _tree.SelectedNode = FindTreeNode(_tree.Nodes, selectId.Value);
                }
            }
            finally
            {
                _tree.EndUpdate();
            }
            UpdateButtonState();
        }

        private static TreeNode CreateNode(FavoriteNode favorite)
        {
            var image = favorite.Kind == FavoriteNodeKind.Folder
                ? BrowserUiConstants.FolderImageIndex
                : BrowserUiConstants.PageImageIndex;
            var node = new TreeNode(favorite.Title, image, image) { Tag = favorite };
            foreach (var child in favorite.Children ?? new List<FavoriteNode>())
            {
                node.Nodes.Add(CreateNode(child));
            }
            return node;
        }

        private void UpdateButtonState()
        {
            var enabled = _tree.SelectedNode?.Tag is FavoriteNode;
            _renameButton.Enabled = enabled;
            _moveButton.Enabled = enabled;
            _deleteButton.Enabled = enabled;
        }

        private void OnNewFolder(object sender, EventArgs args)
        {
            Guid? parent = null;
            if (_tree.SelectedNode?.Tag is FavoriteNode selected && selected.Kind == FavoriteNodeKind.Folder)
            {
                parent = selected.Id;
            }

            using (var dialog = new TextPromptDialog(
                Strings.NewFolderTitle,
                Strings.FolderNamePrompt,
                Strings.NewFolderDefaultName))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                TryAction(() =>
                {
                    var folder = _favorites.AddFolder(parent, dialog.Value);
                    ReloadTree(folder.Id);
                });
            }
        }

        private void OnRename(object sender, EventArgs args)
        {
            if (!(_tree.SelectedNode?.Tag is FavoriteNode selected))
            {
                return;
            }

            using (var dialog = new TextPromptDialog(
                Strings.RenameTitle,
                Strings.NewNamePrompt,
                selected.Title))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    TryAction(() =>
                    {
                        _favorites.Rename(selected.Id, dialog.Value);
                        ReloadTree(selected.Id);
                    });
                }
            }
        }

        private void OnMove(object sender, EventArgs args)
        {
            if (!(_tree.SelectedNode?.Tag is FavoriteNode selected))
            {
                return;
            }

            using (var dialog = new SelectFavoriteFolderDialog(_favorites))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    TryAction(() =>
                    {
                        _favorites.Move(selected.Id, dialog.SelectedFolderId);
                        ReloadTree(selected.Id);
                    });
                }
            }
        }

        private void OnDelete(object sender, EventArgs args)
        {
            if (!(_tree.SelectedNode?.Tag is FavoriteNode selected))
            {
                return;
            }

            var suffix = selected.Kind == FavoriteNodeKind.Folder
                ? Strings.DeleteFolderSuffix
                : string.Empty;
            if (MessageBox.Show(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.DeleteFavoritePromptFormat,
                        selected.Title,
                        suffix),
                    Branding.ProductName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                TryAction(() =>
                {
                    _favorites.Delete(selected.Id);
                    ReloadTree();
                });
            }
        }

        private void TryAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static TreeNode FindTreeNode(TreeNodeCollection nodes, Guid id)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is FavoriteNode favorite && favorite.Id == id)
                {
                    return node;
                }

                var child = FindTreeNode(node.Nodes, id);
                if (child != null)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
