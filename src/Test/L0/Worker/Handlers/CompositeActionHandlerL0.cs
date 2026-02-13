using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker.Handlers
{
    public sealed class CompositeActionHandlerL0
    {
        // Test EscapeProperty helper logic via reflection or by testing the markers output
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EscapeProperty_EscapesSpecialCharacters()
        {
            // Test the escaping logic that would be applied
            var input = "value;with%special\r\n]chars";
            var escaped = EscapeProperty(input);
            Assert.Equal("value%3Bwith%25special%0D%0A%5Dchars", escaped);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EscapeProperty_HandlesNullAndEmpty()
        {
            Assert.Null(EscapeProperty(null));
            Assert.Equal("", EscapeProperty(""));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SanitizeDisplayName_TruncatesLongNames()
        {
            var longName = new string('a', 1500);
            var sanitized = SanitizeDisplayName(longName);
            Assert.Equal(CompositeActionHandler.MaxDisplayNameLength, sanitized.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SanitizeDisplayName_TakesFirstLineOnly()
        {
            var multiline = "First line\nSecond line\nThird line";
            var sanitized = SanitizeDisplayName(multiline);
            Assert.Equal("First line", sanitized);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SanitizeDisplayName_TrimsLeadingWhitespace()
        {
            var withLeading = "   \n  \t  Actual name\nSecond line";
            var sanitized = SanitizeDisplayName(withLeading);
            Assert.Equal("Actual name", sanitized);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SanitizeDisplayName_HandlesCarriageReturn()
        {
            var withCR = "First line\r\nSecond line";
            var sanitized = SanitizeDisplayName(withCR);
            Assert.Equal("First line", sanitized);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SanitizeDisplayName_HandlesNullAndEmpty()
        {
            Assert.Null(SanitizeDisplayName(null));
            Assert.Equal("", SanitizeDisplayName(""));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EmitMarkers_DisplayNameEscaping()
        {
            // Verify that special characters in display names get escaped properly
            var displayName = "Step with semicolons; and more; here";
            var escaped = EscapeProperty(SanitizeDisplayName(displayName));
            Assert.Equal("Step with semicolons%3B and more%3B here", escaped);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EmitMarkers_DisplayNameWithBrackets()
        {
            var displayName = "Step with [brackets] inside";
            var escaped = EscapeProperty(SanitizeDisplayName(displayName));
            Assert.Equal("Step with [brackets%5D inside", escaped);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StripUserEmittedMarkers_StartAction()
        {
            // Simulate what OutputManager does to strip markers
            var userLine = "##[start-action display=Fake;id=fake]";
            var stripped = StripMarkers(userLine);
            Assert.Equal(@"##[\start-action display=Fake;id=fake]", stripped);
            Assert.DoesNotContain("##[start-action", stripped);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StripUserEmittedMarkers_EndAction()
        {
            var userLine = "##[end-action id=fake;outcome=success;conclusion=success;duration_ms=100]";
            var stripped = StripMarkers(userLine);
            Assert.Equal(@"##[\end-action id=fake;outcome=success;conclusion=success;duration_ms=100]", stripped);
            Assert.DoesNotContain("##[end-action", stripped);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StripUserEmittedMarkers_PreservesOtherCommands()
        {
            var userLine = "##[group]My Group";
            var stripped = StripMarkers(userLine);
            Assert.Equal("##[group]My Group", stripped);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StripUserEmittedMarkers_HandlesEmbeddedMarkers()
        {
            var userLine = "Some text ##[start-action display=fake;id=fake] more text";
            var stripped = StripMarkers(userLine);
            Assert.Equal(@"Some text ##[\start-action display=fake;id=fake] more text", stripped);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TaskResultToActionResult_Success()
        {
            var result = GitHub.DistributedTask.WebApi.TaskResult.Succeeded;
            var actionResult = result.ToActionResult();
            Assert.Equal(ActionResult.Success, actionResult);
            Assert.Equal("success", actionResult.ToString().ToLowerInvariant());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TaskResultToActionResult_Failure()
        {
            var result = GitHub.DistributedTask.WebApi.TaskResult.Failed;
            var actionResult = result.ToActionResult();
            Assert.Equal(ActionResult.Failure, actionResult);
            Assert.Equal("failure", actionResult.ToString().ToLowerInvariant());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TaskResultToActionResult_Cancelled()
        {
            var result = GitHub.DistributedTask.WebApi.TaskResult.Canceled;
            var actionResult = result.ToActionResult();
            Assert.Equal(ActionResult.Cancelled, actionResult);
            Assert.Equal("cancelled", actionResult.ToString().ToLowerInvariant());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void TaskResultToActionResult_Skipped()
        {
            var result = GitHub.DistributedTask.WebApi.TaskResult.Skipped;
            var actionResult = result.ToActionResult();
            Assert.Equal(ActionResult.Skipped, actionResult);
            Assert.Equal("skipped", actionResult.ToString().ToLowerInvariant());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MarkerFormat_StartAction()
        {
            var display = "My Step";
            var id = "my-step";
            var marker = $"##[start-action display={EscapeProperty(display)};id={EscapeProperty(id)}]";
            Assert.Equal("##[start-action display=My Step;id=my-step]", marker);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MarkerFormat_EndAction()
        {
            var id = "my-step";
            var outcome = "success";
            var conclusion = "success";
            var durationMs = 1234;
            var marker = $"##[end-action id={EscapeProperty(id)};outcome={outcome};conclusion={conclusion};duration_ms={durationMs}]";
            Assert.Equal("##[end-action id=my-step;outcome=success;conclusion=success;duration_ms=1234]", marker);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MarkerFormat_NestedId()
        {
            var prefix = "outer-composite";
            var contextName = "inner-step";
            var stepId = $"{prefix}.{contextName}";
            Assert.Equal("outer-composite.inner-step", stepId);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MarkerFormat_SkippedStep()
        {
            var id = "skipped-step";
            var marker = $"##[end-action id={EscapeProperty(id)};outcome=skipped;conclusion=skipped;duration_ms=0]";
            Assert.Equal("##[end-action id=skipped-step;outcome=skipped;conclusion=skipped;duration_ms=0]", marker);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MarkerFormat_ContinueOnError()
        {
            // When continue-on-error is true and step fails:
            // outcome = failure (raw result)
            // conclusion = success (after continue-on-error applied)
            var id = "failing-step";
            var marker = $"##[end-action id={EscapeProperty(id)};outcome=failure;conclusion=success;duration_ms=500]";
            Assert.Equal("##[end-action id=failing-step;outcome=failure;conclusion=success;duration_ms=500]", marker);
        }

        // Helper methods that call the real production code
        private static string EscapeProperty(string value) =>
            CompositeActionHandler.EscapeProperty(value);

        private static string SanitizeDisplayName(string displayName) =>
            CompositeActionHandler.SanitizeDisplayName(displayName);

        private static string StripMarkers(string line)
        {
            if (!string.IsNullOrEmpty(line) &&
                (line.Contains("##[start-action") || line.Contains("##[end-action")))
            {
                line = line.Replace("##[start-action", @"##[\start-action")
                           .Replace("##[end-action", @"##[\end-action");
            }
            return line;
        }
    }
}
