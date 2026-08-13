namespace IndianaExpedition.ContextMenus
{
    internal sealed class PageContextMenuModel
    {
        internal PageContextMenuModel(string linkUri, string selectionText)
        {
            LinkUri = linkUri;
            SelectionText = selectionText;
        }

        internal string LinkUri { get; }

        internal string SelectionText { get; }

        internal bool HasLink => !string.IsNullOrWhiteSpace(LinkUri);

        internal bool HasSelection => !string.IsNullOrEmpty(SelectionText);
    }
}
