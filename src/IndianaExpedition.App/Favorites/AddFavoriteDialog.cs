using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.Favorites
{
    internal sealed class AddFavoriteDialog : LunaForm
    {
        private readonly FavoritesService _favorites;
        private readonly string _url;
        private readonly TextBox _nameBox;
        private readonly ComboBox _folderBox;

        internal AddFavoriteDialog(FavoritesService favorites, string title, string url)
        {
            _favorites = favorites;
            _url = url;

            Text = Strings.AddFavoriteTitle;
            SetContentClientSize(430, 178);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            ContentPanel.Controls.Add(new Label { Text = Strings.NameLabel, AutoSize = true, Location = new Point(18, 22) });
            _nameBox = new TextBox { Text = title ?? url, Location = new Point(92, 18), Size = new Size(318, 23) };

            ContentPanel.Controls.Add(new Label { Text = Strings.CreateInLabel, AutoSize = true, Location = new Point(18, 62) });
            _folderBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(92, 58),
                Size = new Size(318, 23)
            };
            PopulateFolders();

            var urlLabel = new Label
            {
                Text = url,
                AutoEllipsis = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(92, 90),
                Size = new Size(318, 20)
            };

            var ok = new XpButton { Text = Strings.AddButton, Location = new Point(246, 132), Size = new Size(78, 27), DialogResult = DialogResult.OK };
            ok.Click += OnAddClicked;
            var cancel = new XpButton { Text = Strings.Cancel, Location = new Point(332, 132), Size = new Size(78, 27), DialogResult = DialogResult.Cancel };

            ContentPanel.Controls.AddRange(new Control[] { _nameBox, _folderBox, urlLabel, ok, cancel });
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void PopulateFolders()
        {
            _folderBox.Items.Add(new FolderChoice(null, Strings.FavoriteRoot));
            foreach (var item in _favorites.Items)
            {
                AddFolderChoices(item, 1);
            }
            _folderBox.SelectedIndex = 0;
        }

        private void AddFolderChoices(FavoriteNode node, int depth)
        {
            if (node.Kind != FavoriteNodeKind.Folder)
            {
                return;
            }

            _folderBox.Items.Add(new FolderChoice(
                node.Id,
                new string(BrowserUiConstants.FolderIndentCharacter, depth) + node.Title));
            foreach (var child in node.Children ?? new List<FavoriteNode>())
            {
                AddFolderChoices(child, depth + 1);
            }
        }

        private void OnAddClicked(object sender, EventArgs args)
        {
            try
            {
                var folder = (FolderChoice)_folderBox.SelectedItem;
                _favorites.AddLink(folder.Id, _nameBox.Text, _url);
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private sealed class FolderChoice
        {
            internal FolderChoice(Guid? id, string text)
            {
                Id = id;
                Text = text;
            }

            internal Guid? Id { get; }

            internal string Text { get; }

            public override string ToString() => Text;
        }
    }
}
