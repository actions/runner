namespace Runner.Server.Azure.Devops
{
    public interface ITaskByNameAndVersionProvider
    {
        TaskMetaData Resolve(string nameAndVersion);
    }
}