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
        private readonly IDocumentStore<FavoritesDocument> _store;
        private FavoritesDocument _document;

        public FavoritesService(string path)
            : this(new AtomicJsonFileStore<FavoritesDocument>(path, FavoritesDocument.CreateDefault))
        {
        }

        internal FavoritesService(IDocumentStore<FavoritesDocument> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
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
            return AddNode(parentFolderId, FavoriteNode.CreateFolder(NormalizeTitle(title)));
        }

        public FavoriteNode AddLink(Guid? parentFolderId, string title, string url)
        {
            return AddNode(
                parentFolderId,
                FavoriteNode.CreateLink(NormalizeTitle(title), NormalizeUrl(url)));
        }

        public void Rename(Guid id, string title)
        {
            Commit(candidate =>
            {
                var node = FindNode(candidate.Items, id);
                if (node == null)
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteNotFound);
                }
                node.Title = NormalizeTitle(title);
            });
        }

        public void Delete(Guid id)
        {
            Commit(candidate =>
            {
                if (!TryDetach(candidate.Items, id, out _))
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteNotFound);
                }
            });
        }

        public void Move(Guid id, Guid? destinationFolderId)
        {
            Commit(candidate =>
            {
                var node = FindNode(candidate.Items, id);
                if (node == null)
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteNotFound);
                }

                FavoriteNode destination = null;
                if (destinationFolderId.HasValue)
                {
                    destination = FindNode(candidate.Items, destinationFolderId.Value);
                    if (destination == null || destination.Kind != FavoriteNodeKind.Folder)
                    {
                        throw new InvalidOperationException(CoreMessages.DestinationFolderNotFound);
                    }
                    if (destination.Id == node.Id || ContainsNode(node, destination.Id))
                    {
                        throw new InvalidOperationException(CoreMessages.CannotMoveFolderIntoDescendant);
                    }
                }

                if (!TryDetach(candidate.Items, id, out var detached))
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteCannotBeMoved);
                }
                (destination == null ? candidate.Items : destination.Children).Add(detached);
            });
        }

        public FavoriteNode Find(Guid id)
        {
            lock (_gate)
            {
                return FindNode(_document.Items, id)?.DeepClone();
            }
        }

        private FavoriteNode AddNode(Guid? parentFolderId, FavoriteNode node)
        {
            Commit(candidate =>
            {
                if (!parentFolderId.HasValue)
                {
                    candidate.Items.Add(node);
                    return;
                }

                var parent = FindNode(candidate.Items, parentFolderId.Value);
                if (parent == null || parent.Kind != FavoriteNodeKind.Folder)
                {
                    throw new InvalidOperationException(CoreMessages.FavoriteFolderNotFound);
                }
                parent.Children.Add(node);
            });
            return node.DeepClone();
        }

        private void Commit(Action<FavoritesDocument> update)
        {
            lock (_gate)
            {
                var candidate = _document.DeepClone();
                update(candidate);
                candidate = Normalize(candidate);
                _store.Save(candidate);
                _document = candidate;
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static FavoritesDocument Normalize(FavoritesDocument document)
        {
            var result = document?.DeepClone() ?? FavoritesDocument.CreateDefault();
            result.SchemaVersion = BrowserDefaults.DataSchemaVersion;
            result.Items = result.Items ?? new List<FavoriteNode>();
            NormalizeNodes(result.Items);
            return result;
        }

        private static void NormalizeNodes(List<FavoriteNode> nodes)
        {
            nodes.RemoveAll(node => node == null);
            var stack = new Stack<FavoriteNode>();
            for (var index = nodes.Count - 1; index >= 0; index--)
            {
                stack.Push(nodes[index]);
            }
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.Id == Guid.Empty)
                {
                    node.Id = Guid.NewGuid();
                }
                node.Title = string.IsNullOrWhiteSpace(node.Title)
                    ? CoreMessages.UntitledFavorite
                    : node.Title.Trim();
                node.Children = node.Children ?? new List<FavoriteNode>();
                if (node.Kind == FavoriteNodeKind.Link)
                {
                    node.Children.Clear();
                }
                else
                {
                    node.Url = null;
                    node.Children.RemoveAll(child => child == null);
                    for (var index = node.Children.Count - 1; index >= 0; index--)
                    {
                        stack.Push(node.Children[index]);
                    }
                }
            }
        }

        private static FavoriteNode FindNode(IEnumerable<FavoriteNode> nodes, Guid id)
        {
            var stack = new Stack<FavoriteNode>(
                (nodes ?? Enumerable.Empty<FavoriteNode>()).Where(node => node != null).Reverse());
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.Id == id)
                {
                    return node;
                }
                var children = node.Children ?? new List<FavoriteNode>();
                for (var index = children.Count - 1; index >= 0; index--)
                {
                    if (children[index] != null)
                    {
                        stack.Push(children[index]);
                    }
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
            var stack = new Stack<List<FavoriteNode>>();
            stack.Push(nodes);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                for (var index = 0; index < current.Count; index++)
                {
                    if (current[index].Id == id)
                    {
                        detached = current[index];
                        current.RemoveAt(index);
                        return true;
                    }
                }
                for (var index = current.Count - 1; index >= 0; index--)
                {
                    if (current[index].Children != null && current[index].Children.Count > 0)
                    {
                        stack.Push(current[index].Children);
                    }
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
