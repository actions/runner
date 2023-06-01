using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Xunit;

namespace GitHub.DistributedTask.ObjectTemplating.Tests
{
    public sealed class TemplateContextL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithTokenAndException()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("(Line: 1, Col: 1): Exception of type 'System.Exception' was thrown.") };

            context.Error(new StringToken(1, 1, 1, "some-token"), new System.Exception());

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithNullTokenAndException()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("System.Exception: Exception of type 'System.Exception' was thrown.") };

            context.Error(null, new System.Exception());

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithTokenAndMessage()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("(Line: 1, Col: 1): message") };

            context.Error(new StringToken(1, 1, 1, "some-token"), "message");

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithNullTokenAndMessage()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("message") };

            context.Error(null, "message");

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithException()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("(Line: 2, Col: 3): Fatal template exception") };
            List<string> expectedTracewriterErrors = new List<string> { "(Line: 2, Col: 3):" };

            context.Error(1, 2, 3, new Exception("Fatal template exception"));

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());
            ListTraceWriter listTraceWriter = (ListTraceWriter)context.TraceWriter;
            List<string> tracewriterErrors = listTraceWriter.GetErrors();

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
            Assert.True(expectedTracewriterErrors.SequenceEqual(tracewriterErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithExceptionAndInnerException()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("(Line: 2, Col: 3): Fatal template exception"),
            new TemplateValidationError("(Line: 2, Col: 3): Inner Exception") };
            List<string> expectedTracewriterErrors = new List<string> { "(Line: 2, Col: 3):" };

            Exception e = new Exception("Fatal template exception", new Exception("Inner Exception"));

            context.Error(1, 2, 3, e);

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());
            ListTraceWriter listTraceWriter = (ListTraceWriter)context.TraceWriter;
            List<string> tracewriterErrors = listTraceWriter.GetErrors();

            Assert.Equal(2, templateValidationErrors.Count);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
            Assert.True(expectedTracewriterErrors.SequenceEqual(tracewriterErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithLineNumbersAndMessage()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("(Line: 2, Col: 3): Fatal template exception") };
            List<string> expectedTracewriterErrors = new List<string> { "(Line: 2, Col: 3): Fatal template exception" };

            context.Error(1, 2, 3, "Fatal template exception");

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());
            ListTraceWriter listTraceWriter = (ListTraceWriter)context.TraceWriter;
            List<string> tracewriterErrors = listTraceWriter.GetErrors();

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
            Assert.True(expectedTracewriterErrors.SequenceEqual(tracewriterErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithNullLineNumbersAndMessage()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("Fatal template exception") };
            List<string> expectedTracewriterErrors = new List<string> { "Fatal template exception" };

            context.Error(null, null, null, "Fatal template exception");

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());
            ListTraceWriter listTraceWriter = (ListTraceWriter)context.TraceWriter;
            List<string> tracewriterErrors = listTraceWriter.GetErrors();

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
            Assert.True(expectedTracewriterErrors.SequenceEqual(tracewriterErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithLineNumbersAndException()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("(Line: 2, Col: 3): Fatal template exception") };
            List<string> expectedTracewriterErrors = new List<string> { "(Line: 2, Col: 3):" };

            context.Error(1, 2, 3, new Exception("Fatal template exception"));

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());
            ListTraceWriter listTraceWriter = (ListTraceWriter)context.TraceWriter;
            List<string> tracewriterErrors = listTraceWriter.GetErrors();

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
            Assert.True(expectedTracewriterErrors.SequenceEqual(tracewriterErrors));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyErrorWithNullLineNumbersAndException()
        {
            TemplateContext context = buildContext();

            List<TemplateValidationError> expectedErrors = new List<TemplateValidationError> { new TemplateValidationError("System.Exception: Fatal template exception") };
            List<string> expectedTracewriterErrors = new List<string> { "" };

            context.Error(null, null, null, new Exception("Fatal template exception"));

            List<TemplateValidationError> templateValidationErrors = toList(context.Errors.GetEnumerator());
            ListTraceWriter listTraceWriter = (ListTraceWriter)context.TraceWriter;
            List<string> tracewriterErrors = listTraceWriter.GetErrors();

            Assert.Single(templateValidationErrors);
            Assert.True(areEqual(expectedErrors, templateValidationErrors));
            Assert.True(expectedTracewriterErrors.SequenceEqual(tracewriterErrors));
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
                TraceWriter = new ListTraceWriter(),
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

        private bool areEqual(List<TemplateValidationError> l1, List<TemplateValidationError> l2)
        {
            if (l1.Count != l2.Count) return false;

            var twoLists = l1.Zip(l2, (l1Error, l2Error) => new { Elem1 = l1Error, Elem2 = l2Error });

            foreach (var elem in twoLists)
            {
                if (elem.Elem1.Message != elem.Elem2.Message) return false;
                if (elem.Elem1.Code != elem.Elem2.Code) return false;
            }
            return true;
        }

        internal sealed class ListTraceWriter : ITraceWriter
        {

            private List<string> errors = new();
            private List<string> infoMessages = new();
            private List<string> verboseMessages = new();
            public void Error(string format, params object[] args)
            {
                errors.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture, $"{format}", args));
            }

            public void Info(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            public void Verbose(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            public List<string> GetErrors()
            {
                return errors;
            }
        }
    }
}
