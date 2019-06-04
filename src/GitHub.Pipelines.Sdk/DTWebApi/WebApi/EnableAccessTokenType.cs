namespace GitHub.DistributedTask.WebApi
{
    public enum EnableAccessTokenType
    {
        None,
        Variable,
        True = Variable,
        SecretVariable,
    }
}
