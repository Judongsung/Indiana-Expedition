namespace IndianaExpedition.Core.Persistence
{
    internal interface IDocumentStore<T> where T : class
    {
        T Load();

        void Save(T value);
    }
}
