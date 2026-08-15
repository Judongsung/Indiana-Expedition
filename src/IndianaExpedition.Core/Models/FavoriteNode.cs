using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Models
{
    public enum FavoriteNodeKind
    {
        Folder = 0,
        Link = 1
    }

    [DataContract]
    public sealed class FavoriteNode
    {
        public FavoriteNode()
        {
            Children = new List<FavoriteNode>();
        }

        [DataMember(Order = 0)]
        public Guid Id { get; set; }

        [DataMember(Order = 1)]
        public FavoriteNodeKind Kind { get; set; }

        [DataMember(Order = 2)]
        public string Title { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Order = 4)]
        public List<FavoriteNode> Children { get; set; }

        public static FavoriteNode CreateFolder(string title)
        {
            return new FavoriteNode
            {
                Id = Guid.NewGuid(),
                Kind = FavoriteNodeKind.Folder,
                Title = title,
                Children = new List<FavoriteNode>()
            };
        }

        public static FavoriteNode CreateLink(string title, string url)
        {
            return new FavoriteNode
            {
                Id = Guid.NewGuid(),
                Kind = FavoriteNodeKind.Link,
                Title = title,
                Url = url,
                Children = new List<FavoriteNode>()
            };
        }

        public FavoriteNode DeepClone()
        {
            var clone = CreateShallowClone(this);
            var stack = new Stack<CloneItem>();
            stack.Push(new CloneItem(this, clone));
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var child in current.Source.Children ?? new List<FavoriteNode>())
                {
                    if (child == null)
                    {
                        continue;
                    }
                    var childClone = CreateShallowClone(child);
                    current.Target.Children.Add(childClone);
                    stack.Push(new CloneItem(child, childClone));
                }
            }
            return clone;
        }

        private static FavoriteNode CreateShallowClone(FavoriteNode source)
        {
            return new FavoriteNode
            {
                Id = source.Id,
                Kind = source.Kind,
                Title = source.Title,
                Url = source.Url,
                Children = new List<FavoriteNode>()
            };
        }

        private sealed class CloneItem
        {
            internal CloneItem(FavoriteNode source, FavoriteNode target)
            {
                Source = source;
                Target = target;
            }

            internal FavoriteNode Source { get; }
            internal FavoriteNode Target { get; }
        }
    }

    [DataContract]
    public sealed class FavoritesDocument
    {
        public FavoritesDocument()
        {
            Items = new List<FavoriteNode>();
        }

        [DataMember(Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 1)]
        public List<FavoriteNode> Items { get; set; }

        public static FavoritesDocument CreateDefault()
        {
            return new FavoritesDocument
            {
                SchemaVersion = BrowserDefaults.DataSchemaVersion,
                Items = new List<FavoriteNode>()
            };
        }

        internal FavoritesDocument DeepClone()
        {
            var clone = new FavoritesDocument { SchemaVersion = SchemaVersion };
            foreach (var item in Items ?? new List<FavoriteNode>())
            {
                if (item != null)
                {
                    clone.Items.Add(item.DeepClone());
                }
            }
            return clone;
        }
    }
}
