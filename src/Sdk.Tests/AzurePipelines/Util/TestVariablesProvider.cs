namespace Runner.Server.Azure.Devops
{
    class TestVariablesProvider : IVariablesProvider
    {
        private IDictionary<string, string> rootVars;

        public TestVariablesProvider(IDictionary<string, string>? rootVars = null)
        {
            if (rootVars == null)
            {
                rootVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            this.rootVars = rootVars;
        }

        public IDictionary<string, string> GetVariablesForEnvironment(string? name = null)
        {
            return rootVars;
        }
    }
}
