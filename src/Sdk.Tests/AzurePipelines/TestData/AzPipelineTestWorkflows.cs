using System.Collections;

namespace Runner.Server.Azure.Devops
{
    public class AzPipelineTestWorkflows : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach(var result in TestGenerator.ResolveWorkflows(TestUtil.GetAzPipelineFolder()))
            {
                yield return new object[] { result };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
