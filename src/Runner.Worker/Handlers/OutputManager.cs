using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Container;
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
        private readonly ContainerInfo _container;
        private readonly IExecutionContext _executionContext;
        private readonly int _failsafe = 50;
        private readonly object _matchersLock = new object();
        private readonly TimeSpan _timeout;
        private IssueMatcher[] _matchers = Array.Empty<IssueMatcher>();
        // Mapping that indicates whether a directory belongs to the workflow repository
        private readonly Dictionary<string, string> _directoryMap = new Dictionary<string, string>();

        public OutputManager(IExecutionContext executionContext, IActionCommandManager commandManager, ContainerInfo container = null)
        {
            _executionContext = executionContext;
            _commandManager = commandManager;
            _container = container ?? executionContext.Global.Container;

            // Recursion failsafe (test override)
            var failsafeString = Environment.GetEnvironmentVariable("RUNNER_TEST_GET_REPOSITORY_PATH_FAILSAFE");
            if (!string.IsNullOrEmpty(failsafeString))
            {
                _failsafe = int.Parse(failsafeString, NumberStyles.None);
            }

            // Determine the timeout
            var timeoutStr = _executionContext.Global.Variables.Get(_timeoutKey);
            if (string.IsNullOrEmpty(timeoutStr) ||
                !TimeSpan.TryParse(timeoutStr, CultureInfo.InvariantCulture, out _timeout) ||
                _timeout <= TimeSpan.Zero)
            {
                timeoutStr = Environment.GetEnvironmentVariable(_timeoutKey);
                if (string.IsNullOrEmpty(timeoutStr) ||
                    !TimeSpan.TryParse(timeoutStr, CultureInfo.InvariantCulture, out _timeout) ||
                    _timeout <= TimeSpan.Zero)
                {
                    _timeout = TimeSpan.FromSeconds(1);
                }
            }

            // Lock
            lock (_matchersLock)
            {
                _executionContext.Add(OnMatcherChanged);
                _matchers = _executionContext.GetMatchers().Select(x => new IssueMatcher(x, _timeout)).ToArray();
            }
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
            var line = e.Data;

            // ## commands
            if (!String.IsNullOrEmpty(line) &&
                (line.IndexOf(ActionCommand.Prefix) >= 0 || line.IndexOf(ActionCommand._commandKey) >= 0))
            {
                // This does not need to be inside of a critical section.
                // The logging queues and command handlers are thread-safe.
                if (_commandManager.TryProcessCommand(_executionContext, line, _container))
                {
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

                            return;
                        }
                    }
                }
            }

            // Regular output
            _executionContext.Output(line);
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
                    var translate = _container != null;

                    // Root using fromPath
                    if (!string.IsNullOrWhiteSpace(match.FromPath) && !Path.IsPathFullyQualified(file))
                    {
                        var fromDirectory = Path.GetDirectoryName(match.FromPath);
                        if (!string.IsNullOrWhiteSpace(fromDirectory))
                        {
                            file = Path.Combine(fromDirectory, file);
                        }
                    }

                    // Root using workspace
                    if (!Path.IsPathFullyQualified(file))
                    {
                        var workspace = _executionContext.GetGitHubContext("workspace");
                        ArgUtil.NotNullOrEmpty(workspace, "workspace");

                        file = Path.Combine(workspace, file);
                        translate = false;
                    }

                    // Remove relative pathing and normalize slashes
                    file = Path.GetFullPath(file);

                    // Translate to host
                    if (translate)
                    {
                        file = _container.TranslateToHostPath(file);
                        file = Path.GetFullPath(file);
                    }

                    // Check whether the file exists
                    if (File.Exists(file))
                    {
                        // Check whether the file is under the workflow repository
                        var repositoryPath = GetRepositoryPath(file);
                        if (!string.IsNullOrEmpty(repositoryPath))
                        {
                            // Get the relative file path
                            var relativePath = file.Substring(repositoryPath.Length).TrimStart(Path.DirectorySeparatorChar);

                            // Prefer `/` on all platforms
                            issue.Data["file"] = relativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        }
                        else
                        {
                            _executionContext.Debug($"Dropping file value '{file}'. Path is not under the workflow repo.");
                        }
                    }
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

        private string GetRepositoryPath(string filePath, int recursion = 0)
        {
            // Prevent the cache from growing too much
            if (_directoryMap.Count > 100)
            {
                _directoryMap.Clear();
            }

            // Empty directory means we hit the root of the drive
            var directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directoryPath) || recursion > _failsafe)
            {
                return null;
            }

            // Check the cache
            if (_directoryMap.TryGetValue(directoryPath, out string repositoryPath))
            {
                return repositoryPath;
            }

            try
            {
                // Check if .git/config exists
                var gitConfigPath = Path.Combine(directoryPath, ".git", "config");
                if (File.Exists(gitConfigPath))
                {
                    // Check if the config contains the workflow repository url
                    var serverUrl = _executionContext.GetGitHubContext("server_url");
                    serverUrl = !string.IsNullOrEmpty(serverUrl) ? serverUrl : "https://github.com";
                    var host = new Uri(serverUrl, UriKind.Absolute).Host;
                    var nameWithOwner = _executionContext.GetGitHubContext("repository");
                    var patterns = new[] {
                        $"url = {serverUrl}/{nameWithOwner}",
                        $"url = git@{host}:{nameWithOwner}.git",
                    };
                    var content = File.ReadAllText(gitConfigPath);
                    foreach (var line in content.Split("\n").Select(x => x.Trim()))
                    {
                        foreach (var pattern in patterns)
                        {
                            if (String.Equals(line, pattern, StringComparison.OrdinalIgnoreCase))
                            {
                                repositoryPath = directoryPath;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Recursive call
                    repositoryPath = GetRepositoryPath(directoryPath, recursion + 1);
                }
            }
            catch (Exception ex)
            {
                _executionContext.Debug($"Error when attempting to determine whether the path '{filePath}' is under the workflow repository: {ex.Message}");
            }

            _directoryMap[directoryPath] = repositoryPath;
            return repositoryPath;
        }
    }
}
