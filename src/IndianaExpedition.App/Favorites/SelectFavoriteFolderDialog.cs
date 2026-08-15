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
    internal sealed class SelectFavoriteFolderDialog : LunaForm
    {
        private readonly ComboBox _folderBox;

        internal SelectFavoriteFolderDialog(FavoritesService favorites)
        {
            Text = Strings.MoveToFolderTitle;
            SetContentClientSize(390, 126);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            ContentPanel.Controls.Add(new Label { Text = Strings.DestinationFolderLabel, AutoSize = true, Location = new Point(16, 20) });
            _folderBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(110, 16),
                Size = new Size(260, 23)
            };
            foreach (var choice in FavoriteProjection.BuildFolderChoices(favorites.Items))
            {
                _folderBox.Items.Add(choice);
            }
            _folderBox.SelectedIndex = 0;

            var ok = new XpButton { Text = Strings.Ok, DialogResult = DialogResult.OK, Location = new Point(206, 82), Size = new Size(78, 26) };
            var cancel = new XpButton { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, Location = new Point(292, 82), Size = new Size(78, 26) };
            ContentPanel.Controls.AddRange(new Control[] { _folderBox, ok, cancel });
            AcceptButton = ok;
            CancelButton = cancel;
        }

        internal Guid? SelectedFolderId => ((FavoriteFolderChoice)_folderBox.SelectedItem).Id;
    }
}
