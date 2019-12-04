using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using DTWebApi = GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Worker.Handlers
{
    public sealed class OutputManager : IDisposable
    {
        private const string _colorCodePrefix = "\033[";
        private const int _maxAttempts = 3;
        private const string _timeoutKey = "GITHUB_ACTIONS_RUNNER_ISSUE_MATCHER_TIMEOUT";
        private static readonly Regex _colorCodeRegex = new Regex(@"\x0033\[[0-9;]*m?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly IActionCommandManager _commandManager;
        private readonly IExecutionContext _executionContext;
        private readonly object _matchersLock = new object();
        private readonly TimeSpan _timeout;
        private IssueMatcher[] _matchers = Array.Empty<IssueMatcher>();
        // Mapping that indicates whether a directory belongs to the workflow repository
        private readonly Dictionary<string, bool> _directoryMap = new Dictionary<string, bool>();

        public OutputManager(IExecutionContext executionContext, IActionCommandManager commandManager)
        {
            //executionContext.Debug("ENTERING OutputManager ctor");
            _executionContext = executionContext;
            _commandManager = commandManager;

            //_executionContext.Debug("OutputManager ctor - determine timeout from variable");
            // Determine the timeout
            var timeoutStr = _executionContext.Variables.Get(_timeoutKey);
            if (string.IsNullOrEmpty(timeoutStr) ||
                !TimeSpan.TryParse(timeoutStr, CultureInfo.InvariantCulture, out _timeout) ||
                _timeout <= TimeSpan.Zero)
            {
                //_executionContext.Debug("OutputManager ctor - determine timeout from env var");
                timeoutStr = Environment.GetEnvironmentVariable(_timeoutKey);
                if (string.IsNullOrEmpty(timeoutStr) ||
                    !TimeSpan.TryParse(timeoutStr, CultureInfo.InvariantCulture, out _timeout) ||
                    _timeout <= TimeSpan.Zero)
                {
                    //_executionContext.Debug("OutputManager ctor - set timeout to default");
                    _timeout = TimeSpan.FromSeconds(1);
                }
            }

            //_executionContext.Debug("OutputManager ctor - adding matchers");
            // Lock
            lock (_matchersLock)
            {
                //_executionContext.Debug("OutputManager ctor - adding OnMatcherChanged");
                _executionContext.Add(OnMatcherChanged);
                //_executionContext.Debug("OutputManager ctor - getting matchers");
                _matchers = _executionContext.GetMatchers().Select(x => new IssueMatcher(x, _timeout)).ToArray();
            }
            //_executionContext.Debug("LEAVING OutputManager ctor");
        }

        public void Dispose()
        {
            try
            {
                _executionContext.Remove(OnMatcherChanged);
            }
            catch
            {
            }
        }

        public void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            //_executionContext.Debug("ENTERING OutputManager OnDataReceived");
            var line = e.Data;

            // ## commands
            if (!String.IsNullOrEmpty(line) &&
                (line.IndexOf(ActionCommand.Prefix) >= 0 || line.IndexOf(ActionCommand._commandKey) >= 0))
            {
                // This does not need to be inside of a critical section.
                // The logging queues and command handlers are thread-safe.
                if (_commandManager.TryProcessCommand(_executionContext, line))
                {
                    //_executionContext.Debug("LEAVING OutputManager OnDataReceived - command processed");
                    return;
                }
            }

            // Problem matchers
            if (_matchers.Length > 0)
            {
                // Copy the reference
                var matchers = _matchers;

                // Strip color codes
                var stripped = line.Contains(_colorCodePrefix) ? _colorCodeRegex.Replace(line, string.Empty) : line;

                foreach (var matcher in matchers)
                {
                    IssueMatch match = null;
                    for (var attempt = 1; attempt <= _maxAttempts; attempt++)
                    {
                        // Match
                        try
                        {
                            match = matcher.Match(stripped);

                            break;
                        }
                        catch (RegexMatchTimeoutException ex)
                        {
                            if (attempt < _maxAttempts)
                            {
                                // Debug
                                _executionContext.Debug($"Timeout processing issue matcher '{matcher.Owner}' against line '{stripped}'. Exception: {ex.ToString()}");
                            }
                            else
                            {
                                // Warn
                                _executionContext.Warning($"Removing issue matcher '{matcher.Owner}'. Matcher failed {_maxAttempts} times. Error: {ex.Message}");

                                // Remove
                                Remove(matcher);
                            }
                        }
                    }

                    if (match != null)
                    {
                        // Reset other matchers
                        foreach (var otherMatcher in matchers.Where(x => !object.ReferenceEquals(x, matcher)))
                        {
                            otherMatcher.Reset();
                        }

                        // Convert to issue
                        var issue = ConvertToIssue(match);

                        if (issue != null)
                        {
                            // Log issue
                            _executionContext.AddIssue(issue, stripped);

                            //_executionContext.Debug("LEAVING OutputManager OnDataReceived - issue logged");
                            return;
                        }
                    }
                }
            }

            // Regular output
            _executionContext.Output(line);
            //_executionContext.Debug("LEAVING OutputManager OnDataReceived");
        }

        private void OnMatcherChanged(object sender, MatcherChangedEventArgs e)
        {
            // Lock
            lock (_matchersLock)
            {
                var newMatchers = new List<IssueMatcher>();

                // Prepend
                if (e.Config.Patterns.Length > 0)
                {
                    newMatchers.Add(new IssueMatcher(e.Config, _timeout));
                }

                // Add existing non-matching
                newMatchers.AddRange(_matchers.Where(x => !string.Equals(x.Owner, e.Config.Owner, StringComparison.OrdinalIgnoreCase)));

                // Store
                _matchers = newMatchers.ToArray();
            }
        }

        private void Remove(IssueMatcher matcher)
        {
            // Lock
            lock (_matchersLock)
            {
                var newMatchers = new List<IssueMatcher>();

                // Match by object reference, not by owner name
                newMatchers.AddRange(_matchers.Where(x => !object.ReferenceEquals(x, matcher)));

                // Store
                _matchers = newMatchers.ToArray();
            }
        }

        private DTWebApi.Issue ConvertToIssue(IssueMatch match)
        {
            // Validate the message
            if (string.IsNullOrWhiteSpace(match.Message))
            {
                _executionContext.Debug("Skipping logging an issue for the matched line because the message is empty.");
                return null;
            }

            // Validate the severity
            DTWebApi.IssueType issueType;
            if (string.IsNullOrEmpty(match.Severity) || string.Equals(match.Severity, "error", StringComparison.OrdinalIgnoreCase))
            {
                issueType = DTWebApi.IssueType.Error;
            }
            else if (string.Equals(match.Severity, "warning", StringComparison.OrdinalIgnoreCase))
            {
                issueType = DTWebApi.IssueType.Warning;
            }
            else
            {
                _executionContext.Debug($"Skipped logging an issue for the matched line because the severity '{match.Severity}' is not supported.");
                return null;
            }

            var issue = new DTWebApi.Issue
            {
                Message = match.Message,
                Type = issueType,
            };

            // Line
            if (!string.IsNullOrEmpty(match.Line))
            {
                if (int.TryParse(match.Line, NumberStyles.None, CultureInfo.InvariantCulture, out var line))
                {
                    issue.Data["line"] = line.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    _executionContext.Debug($"Unable to parse line number '{match.Line}'");
                }
            }

            // Column
            if (!string.IsNullOrEmpty(match.Column))
            {
                if (int.TryParse(match.Column, NumberStyles.None, CultureInfo.InvariantCulture, out var column))
                {
                    issue.Data["col"] = column.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    _executionContext.Debug($"Unable to parse column number '{match.Column}'");
                }
            }

            // Code
            if (!string.IsNullOrWhiteSpace(match.Code))
            {
                issue.Data["code"] = match.Code.Trim();
            }

            // File
            try
            {
                if (!string.IsNullOrWhiteSpace(match.File))
                {
                    var file = match.File;

                    // Root using fromPath
                    if (!string.IsNullOrWhiteSpace(match.FromPath) && !Path.IsPathRooted(file))
                    {
                        var fromDirectory = Path.GetDirectoryName(match.FromPath);
                        if (!string.IsNullOrWhiteSpace(fromDirectory))
                        {
                            file = Path.Combine(fromDirectory, file);
                        }
                    }

                    // Root using workspace
                    if (!Path.IsPathRooted(file))
                    {
                        var workspace = _executionContext.GetGitHubContext("workspace");
                        ArgUtil.NotNullOrEmpty(workspace, "workspace");

                        file = Path.Combine(workspace, file);
                    }

                    // Normalize slashes
                    file = file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    // File exists
                    if (File.Exists(file))
                    {
                        // Repository path
                        var repositoryPath = _executionContext.GetGitHubContext("workspace");
                        ArgUtil.NotNullOrEmpty(repositoryPath, nameof(repositoryPath));

                        // Normalize slashes
                        repositoryPath = repositoryPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                        if (!file.StartsWith(repositoryPath, IOUtil.FilePathStringComparison))
                        {
                            // File is not under repo
                            _executionContext.Debug($"Dropping file value '{file}'. Path is not under the repo.");
                        }
                        else
                        {
                            // Prefer `/` on all platforms
                            issue.Data["file"] = file.Substring(repositoryPath.Length).TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        }
                    }
                    // File does not exist
                    else
                    {
                        _executionContext.Debug($"Dropping file value '{file}'. Path does not exist");
                    }
                }
            }
            catch (Exception ex)
            {
                _executionContext.Debug($"Dropping file value '{match.File}' and fromPath value '{match.FromPath}'. Exception during validation: {ex.ToString()}");
            }

            return issue;
        }

        private bool IsWorkflowRepositoryFile(string filePath, int recursion = 0)
        {
            // Prevent the cache from growing too much
            if (_directoryMap.Count > 100)
            {
                _directoryMap.Clear();
            }

            var directoryPath = Path.GetDirectoryName(filePath);

            // Empty directory means we hit the root of the drive
            // todo: Test on Windows
            const int failsafe = 50;
            if (string.IsNullOrEmpty(directoryPath) || recursion > failsafe)
            {
                return false;
            }

            // Check the cache
            if (_directoryMap.TryGetValue(directoryPath, out bool isWorkflowDirectory))
            {
                return isWorkflowDirectory;
            }

            try
            {
                var qualifiedRepository = _executionContext.GetGitHubContext("repository");
                var configMatch =  $"url = https://github.com/${qualifiedRepository}";
                var gitConfigPath = Path.Combine(directoryPath, ".git", "config");
                if (File.Exists(gitConfigPath))
                {
                    var content = File.ReadAllText(gitConfigPath);
                    foreach (var line in content.Split("\n").Select(x => x.Trim()))
                    {
                        if (String.Equals(line, configMatch, StringComparison.OrdinalIgnoreCase))
                        {
                            isWorkflowDirectory = true;
                            break;
                        }
                    }
                }
                else
                {
                    // Recursive call
                    isWorkflowDirectory = IsWorkflowRepositoryFile(directoryPath, recursion + 1);
                }
            }
            catch
            {
            }

            _directoryMap[directoryPath] = isWorkflowDirectory;
            return isWorkflowDirectory;
        }
    }
}
