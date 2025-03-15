using Xunit.Abstractions;

namespace Runner.Server.Azure.Devops
{
    public class AzurePipelinesYamlValidationTests
    {
        private TestContext Context;

        public AzurePipelinesYamlValidationTests(ITestOutputHelper output)
        {
            Context = TestContext.Create(Directory.GetCurrentDirectory()).AddOutputToTest(output);
        }

        [Theory]
        [ClassData(typeof(AzPipelineTestWorkflows))]
        public void ValidateYamlFormat(TestWorkflow workflow)
        {
            // arrange
            Context.SetWorkingDirectory(TestUtil.GetAzPipelineFolder(workflow.WorkingDirectory));
            foreach(var localRepo in workflow.LocalRepository)
            {
                var repositoryAndRef = localRepo.Split("=");
                Context.AddRepo(repositoryAndRef[0], TestUtil.GetAzPipelineFolder(repositoryAndRef[1]));
            }

            // act
            var act = new Action(() => {
                if (workflow.AutoCompletion?.Length > 0)
                {
                    Context.AutoComplete(workflow.File, workflow.Row, workflow.Column, workflow.AutoCompletion);
                }
                else if (workflow.ValidateSyntax)
                {
                    Context.ValidateSyntax(workflow.File);
                }
                else
                {
                    Context.Evaluate(workflow.File).ToYaml();
                }
            });

            // assert
            if (workflow.ExpectedException != null)
            {
                var message = Should.Throw(act, workflow.ExpectedException).Message;

                if (workflow.ExpectedErrorMessage != null)
                {
                    message.ShouldContain(workflow.ExpectedErrorMessage);
                }

            }
            else
            {
                act.Invoke();    
            }            
        }

        [Fact]
        public void TestWorkflowResolve()
        {
            var results = TestGenerator.ResolveWorkflows(TestUtil.GetAzPipelineFolder()).ToArray();
            results.ShouldNotBeNull();
        }
    }
}
