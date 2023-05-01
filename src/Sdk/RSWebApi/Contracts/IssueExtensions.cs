using GitHub.DistributedTask.WebApi;

namespace Sdk.RSWebApi.Contracts
{
    public static class IssueExtensions
    {
        public static Annotation? ToAnnotation(this Issue issue)
        {
            var issueMessage = issue.Message;
            if (string.IsNullOrWhiteSpace(issueMessage))
            {
                if (!issue.Data.TryGetValue(RunIssueKeys.Message, out issueMessage) || string.IsNullOrWhiteSpace(issueMessage))
                {
                    return null;
                }
            }

            var annotationLevel = GetAnnotationLevel(issue.Type);
            var path = GetFilePath(issue);
            var lineNumber = GetAnnotationNumber(issue, RunIssueKeys.Line) ?? 0;
            var endLineNumber = GetAnnotationNumber(issue, RunIssueKeys.EndLine) ?? lineNumber;
            var columnNumber = GetAnnotationNumber(issue, RunIssueKeys.Col) ?? 0;
            var endColumnNumber = GetAnnotationNumber(issue, RunIssueKeys.EndColumn) ?? columnNumber;
            var logLineNumber = GetAnnotationNumber(issue, RunIssueKeys.LogLineNumber) ?? 0;

            if (path == null && lineNumber == 0 && logLineNumber != 0)
            {
                lineNumber = logLineNumber;
                endLineNumber = logLineNumber;
            }

            return new Annotation
            {
                Level = annotationLevel,
                Message = issueMessage,
                Path = path,
                StartLine = lineNumber,
                EndLine = endLineNumber,
                StartColumn = columnNumber,
                EndColumn = endColumnNumber,
            };
        }

        private static AnnotationLevel GetAnnotationLevel(IssueType issueType)
        {
            switch (issueType)
            {
                case IssueType.Error:
                    return AnnotationLevel.FAILURE;
                case IssueType.Warning:
                    return AnnotationLevel.WARNING;
                case IssueType.Notice:
                    return AnnotationLevel.NOTICE;
                default:
                    return AnnotationLevel.UNKNOWN;
            }
        }

        private static int? GetAnnotationNumber(Issue issue, string key)
        {
            if (issue.Data.TryGetValue(key, out var numberString) &&
                int.TryParse(numberString, out var number))
            {
                return number;
            }

            return null;
        }

        private static string GetAnnotationField(Issue issue, string key)
        {
            if (issue.Data.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        private static string GetFilePath(Issue issue)
        {
            if (issue.Data.TryGetValue(RunIssueKeys.File, out var path) &&
                !string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            return null;
        }
    }
}
