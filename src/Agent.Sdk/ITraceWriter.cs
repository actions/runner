namespace Agent.Sdk
{
    public interface ITraceWriter
    {
        void Info(string message);
        void Verbose(string message);
    }
}
