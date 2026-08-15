using System;
using System.Collections.Generic;
using System.Linq;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Favorites
{
    internal sealed class FavoriteProjectionNode
    {
        internal FavoriteProjectionNode(FavoriteNode source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Children = new List<FavoriteProjectionNode>();
        }

        internal FavoriteNode Source { get; }
        internal List<FavoriteProjectionNode> Children { get; }
    }

    internal sealed class FavoriteFolderChoice
    {
        internal FavoriteFolderChoice(Guid? id, string text)
        {
            Id = id;
            Text = text;
        }

        internal Guid? Id { get; }
        internal string Text { get; }
        public override string ToString() => Text;
    }

    internal static class FavoriteProjection
    {
        internal static IReadOnlyList<FavoriteProjectionNode> Build(
            IEnumerable<FavoriteNode> source)
        {
            var roots = new List<FavoriteProjectionNode>();
            var stack = new Stack<TraversalItem>();
            foreach (var item in (source ?? Enumerable.Empty<FavoriteNode>())
                .Where(node => node != null)
                .Reverse())
            {
                stack.Push(new TraversalItem(item, roots));
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var projected = new FavoriteProjectionNode(current.Node);
                current.Target.Add(projected);
                foreach (var child in (current.Node.Children ?? new List<FavoriteNode>())
                    .Where(node => node != null)
                    .Reverse())
                {
                    stack.Push(new TraversalItem(child, projected.Children));
                }
            }
            return roots;
        }

        internal static IReadOnlyList<FavoriteFolderChoice> BuildFolderChoices(
            IEnumerable<FavoriteNode> source)
        {
            var result = new List<FavoriteFolderChoice>
            {
                new FavoriteFolderChoice(null, Strings.FavoriteRoot)
            };
            var stack = new Stack<FolderTraversalItem>();
            foreach (var item in (source ?? Enumerable.Empty<FavoriteNode>())
                .Where(node => node != null)
                .Reverse())
            {
                stack.Push(new FolderTraversalItem(item, 1));
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.Node.Kind != FavoriteNodeKind.Folder)
                {
                    continue;
                }
                result.Add(new FavoriteFolderChoice(
                    current.Node.Id,
                    new string(BrowserUiConstants.FolderIndentCharacter, current.Depth) + current.Node.Title));
                foreach (var child in (current.Node.Children ?? new List<FavoriteNode>())
                    .Where(node => node != null)
                    .Reverse())
                {
                    stack.Push(new FolderTraversalItem(child, current.Depth + 1));
                }
            }
            return result;
        }

        private sealed class TraversalItem
        {
            internal TraversalItem(FavoriteNode node, List<FavoriteProjectionNode> target)
            {
                Node = node;
                Target = target;
            }
            internal FavoriteNode Node { get; }
            internal List<FavoriteProjectionNode> Target { get; }
        }

        private sealed class FolderTraversalItem
        {
            internal FolderTraversalItem(FavoriteNode node, int depth)
            {
                Node = node;
                Depth = depth;
            }
            internal FavoriteNode Node { get; }
            internal int Depth { get; }
        }
    }
}
