using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;
using Sdk.RSWebApi.Contracts;
using Xunit;

namespace GitHub.Actions.RunService.WebApi.Tests;

public sealed class AnnotationsL0
{
    [Fact]
    public void ToAnnotation_ValidIssueWithMessage_ReturnsAnnotation()
    {
        var issue = new Issue
        {
            Type = IssueType.Error,
            Message = "An error occurred",
            IsInfrastructureIssue = true
        };

        issue.Data.Add(RunIssueKeys.File, "test.txt");
        issue.Data.Add(RunIssueKeys.Line, "5");
        issue.Data.Add(RunIssueKeys.Col, "10");
        issue.Data.Add(RunIssueKeys.EndLine, "8");
        issue.Data.Add(RunIssueKeys.EndColumn, "20");
        issue.Data.Add(RunIssueKeys.LogLineNumber, "2");

        var annotation = issue.ToAnnotation();

        Assert.NotNull(annotation);
        Assert.Equal(AnnotationLevel.FAILURE, annotation.Value.Level);
        Assert.Equal("An error occurred", annotation.Value.Message);
        Assert.Equal("test.txt", annotation.Value.Path);
        Assert.Equal(5, annotation.Value.StartLine);
        Assert.Equal(8, annotation.Value.EndLine);
        Assert.Equal(10, annotation.Value.StartColumn);
        Assert.Equal(20, annotation.Value.EndColumn);
    }

    [Fact]
    public void ToAnnotation_ValidIssueWithEmptyMessage_ReturnsNull()
    {
        var issue = new Issue
        {
            Type = IssueType.Warning,
            Message = string.Empty
        };

        var annotation = issue.ToAnnotation();

        Assert.Null(annotation);
    }

    [Fact]
    public void ToAnnotation_ValidIssueWithMessageInData_ReturnsAnnotation()
    {
        var issue = new Issue
        {
            Type = IssueType.Warning,
            Message = string.Empty,
        };

        issue.Data.Add(RunIssueKeys.Message, "A warning occurred");

        var annotation = issue.ToAnnotation();

        Assert.NotNull(annotation);
        Assert.Equal(AnnotationLevel.WARNING, annotation.Value.Level);
        Assert.Equal("A warning occurred", annotation.Value.Message);
    }
}
