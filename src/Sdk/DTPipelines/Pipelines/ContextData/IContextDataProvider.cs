namespace GitHub.DistributedTask.Pipelines.ContextData
{
    public interface IContextDataProvider
    {
        DictionaryContextData ToContextData();
    }
}
