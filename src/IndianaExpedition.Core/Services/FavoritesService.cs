using System;
using System.Collections.Generic;
using System.Linq;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Persistence;

namespace IndianaExpedition.Core.Services
{
    public sealed class FavoritesService
    {
        private readonly object _gate = new object();
        private readonly AtomicJsonFileStore<FavoritesDocument> _store;
        private FavoritesDocument _document;

        public FavoritesService(string path)
        {
            _store = new AtomicJsonFileStore<FavoritesDocument>(path, FavoritesDocument.CreateDefault);
            _document = Normalize(_store.Load());
        }

        public event EventHandler Changed;

        public IReadOnlyList<FavoriteNode> Items
        {
            get
            {
                lock (_gate)
                {
                    return _document.Items.Select(item => item.DeepClone()).ToList();
                }
            }
        }

        public FavoriteNode AddFolder(Guid? parentFolderId, string title)
        {
            var folder = FavoriteNode.CreateFolder(NormalizeTitle(title));
            AddNode(parentFolderId, folder);
            return folder.DeepClone();
        }

        public FavoriteNode AddLink(Guid? parentFolderId, string title, string url)
        {
            var normalizedUrl = NormalizeUrl(url);
            var link = FavoriteNode.CreateLink(NormalizeTitle(title), normalizedUrl);
            AddNode(parentFolderId, link);
            return link.DeepClone();
        }

        public void Rename(Guid id, string title)
        {
            lock (_gate)
            {
                var node = FindNode(_document.Items, id);
                if (node == null)
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteNotFound);
                }

                node.Title = NormalizeTitle(title);
                SaveLocked();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Delete(Guid id)
        {
            lock (_gate)
            {
                if (!TryDetach(_document.Items, id, out _))
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteNotFound);
                }

                SaveLocked();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Move(Guid id, Guid? destinationFolderId)
        {
            lock (_gate)
            {
                var node = FindNode(_document.Items, id);
                if (node == null)
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteNotFound);
                }

                FavoriteNode destination = null;
                if (destinationFolderId.HasValue)
                {
                    destination = FindNode(_document.Items, destinationFolderId.Value);
                    if (destination == null || destination.Kind != FavoriteNodeKind.Folder)
                    {
                        throw new InvalidOperationException(CoreMessages.DestinationFolderNotFound);
                    }

                    if (destination.Id == node.Id || ContainsNode(node, destination.Id))
                    {
                        throw new InvalidOperationException(CoreMessages.CannotMoveFolderIntoDescendant);
                    }
                }

                if (!TryDetach(_document.Items, id, out var detached))
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteCannotBeMoved);
                }

                var target = destination == null ? _document.Items : destination.Children;
                target.Add(detached);
                SaveLocked();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public FavoriteNode Find(Guid id)
        {
            lock (_gate)
            {
                return FindNode(_document.Items, id)?.DeepClone();
            }
        }

        private void AddNode(Guid? parentFolderId, FavoriteNode node)
        {
            lock (_gate)
            {
                if (parentFolderId.HasValue)
                {
                    var parent = FindNode(_document.Items, parentFolderId.Value);
                    if (parent == null || parent.Kind != FavoriteNodeKind.Folder)
                    {
                        throw new InvalidOperationException(CoreMessages.FavoriteFolderNotFound);
                    }

                    parent.Children.Add(node);
                }
                else
                {
                    _document.Items.Add(node);
                }

                SaveLocked();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void SaveLocked()
        {
            _store.Save(_document);
        }

        private static FavoritesDocument Normalize(FavoritesDocument document)
        {
            var result = document ?? FavoritesDocument.CreateDefault();
            result.SchemaVersion = BrowserDefaults.DataSchemaVersion;
            result.Items = result.Items ?? new List<FavoriteNode>();
            NormalizeNodes(result.Items);
            return result;
        }

        private static void NormalizeNodes(IEnumerable<FavoriteNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Id == Guid.Empty)
                {
                    node.Id = Guid.NewGuid();
                }

                node.Title = string.IsNullOrWhiteSpace(node.Title) ? CoreMessages.UntitledFavorite : node.Title.Trim();
                node.Children = node.Children ?? new List<FavoriteNode>();
                if (node.Kind == FavoriteNodeKind.Link)
                {
                    node.Children.Clear();
                }
                else
                {
                    node.Url = null;
                    NormalizeNodes(node.Children);
                }
            }
        }

        private static FavoriteNode FindNode(IEnumerable<FavoriteNode> nodes, Guid id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id)
                {
                    return node;
                }

                var child = FindNode(node.Children ?? Enumerable.Empty<FavoriteNode>(), id);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static bool ContainsNode(FavoriteNode root, Guid id)
        {
            return FindNode(root.Children ?? Enumerable.Empty<FavoriteNode>(), id) != null;
        }

        private static bool TryDetach(List<FavoriteNode> nodes, Guid id, out FavoriteNode detached)
        {
            for (var index = 0; index < nodes.Count; index++)
            {
                if (nodes[index].Id == id)
                {
                    detached = nodes[index];
                    nodes.RemoveAt(index);
                    return true;
                }

                var children = nodes[index].Children;
                if (children != null && TryDetach(children, id, out detached))
                {
                    return true;
                }
            }

            detached = null;
            return false;
        }

        private static string NormalizeTitle(string title)
        {
            var value = (title ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                throw new ArgumentException(CoreMessages.FavoriteNameRequired, nameof(title));
            }

            return value;
        }

        private static string NormalizeUrl(string url)
        {
            var value = (url ?? string.Empty).Trim();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException(CoreMessages.InvalidFavoriteUrl, nameof(url));
            }

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps &&
                uri.Scheme != Uri.UriSchemeFile)
            {
                throw new ArgumentException(CoreMessages.UnsupportedFavoriteUrl, nameof(url));
            }

            return uri.AbsoluteUri;
        }
    }
}
