using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Xunit;

namespace GitHub.DistributedTask.ObjectTemplating.Tests
{
    public sealed class TemplateContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyError()
        {
            TemplateContext context = buildContext();
            TemplateToken value = new StringToken(1, 1, 1, "some-token");
            System.Exception ex = new System.Exception();

            List<TemplateValidationError> expectedErrors = new();
            expectedErrors.Add(new TemplateValidationError("(Line: 1, Col: 1): Exception of type 'System.Exception' was thrown."));

            context.Error(value, ex);

 
            Assert.True(expectedErrors.SequenceEqual(toList(context.Errors.GetEnumerator())));


            Assert.True(true);
        }

        private TemplateContext buildContext()
        {
            return new TemplateContext
            {
                // CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(10, int.MaxValue), // Don't truncate error messages otherwise we might not scrub secrets correctly
                Memory = new TemplateMemory(
                maxDepth: 100,
                maxEvents: 1000000,
                maxBytes: 10 * 1024 * 1024),
                Schema = null,
                TraceWriter = new EmptyTraceWriter(),
            };
        }

        private List<TemplateValidationError> toList(IEnumerator<TemplateValidationError> enumerator)
        {
            List<TemplateValidationError> result = new();
            while (enumerator.MoveNext())
            {
                TemplateValidationError err = enumerator.Current;
                result.Add(err);
            }
            return result;
        }
    }
}
