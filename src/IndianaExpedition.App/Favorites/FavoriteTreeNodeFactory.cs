using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;

namespace IndianaExpedition.Favorites
{
    internal static class FavoriteTreeNodeFactory
    {
        internal static IReadOnlyList<TreeNode> Build(
            IEnumerable<FavoriteProjectionNode> source,
            Action<FavoriteNode, TreeNode> created = null)
        {
            var roots = new List<TreeNode>();
            var items = (source ?? Enumerable.Empty<FavoriteProjectionNode>()).ToList();
            var stack = new Stack<BuildItem>();
            for (var index = items.Count - 1; index >= 0; index--)
            {
                stack.Push(new BuildItem(items[index], null));
            }

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                var imageIndex = item.Projection.Source.Kind == FavoriteNodeKind.Folder
                    ? BrowserUiConstants.FolderImageIndex
                    : BrowserUiConstants.PageImageIndex;
                var node = new TreeNode(
                    item.Projection.Source.Title,
                    imageIndex,
                    imageIndex)
                {
                    Tag = item.Projection.Source
                };
                if (item.Parent == null)
                {
                    roots.Add(node);
                }
                else
                {
                    item.Parent.Nodes.Add(node);
                }
                created?.Invoke(item.Projection.Source, node);

                for (var index = item.Projection.Children.Count - 1; index >= 0; index--)
                {
                    stack.Push(new BuildItem(item.Projection.Children[index], node));
                }
            }
            return roots;
        }

        private sealed class BuildItem
        {
            internal BuildItem(FavoriteProjectionNode projection, TreeNode parent)
            {
                Projection = projection ?? throw new ArgumentNullException(nameof(projection));
                Parent = parent;
            }

            internal FavoriteProjectionNode Projection { get; }
            internal TreeNode Parent { get; }
        }
    }
}
