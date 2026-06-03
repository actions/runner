using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GitHub.Runner.Worker.Dap
{
    internal sealed class JobExecutionView
    {
        private const string _sourceFileName = "execution.yml";

        private readonly object _lock = new object();
        private readonly List<SourceEntry> _preEntries = new List<SourceEntry>();
        private readonly List<SourceEntry> _mainEntries = new List<SourceEntry>();
        private readonly List<SourceEntry> _postEntries = new List<SourceEntry>();
        private readonly List<StepLine> _lineByStep = new List<StepLine>();
        private string _content;
        private int _completeJobLine;

        public JobExecutionView(
            string jobId,
            IEnumerable<IStep> steps,
            IEnumerable<IStep> initialPostSteps,
            IEnumerable<PredictedPostStep> predictedPostSteps = null)
        {
            JobId = string.IsNullOrWhiteSpace(jobId) ? "job" : jobId;

            _preEntries.Add(new SourceEntry("Set up job"));
            AddSteps(steps);
            AddPredictedPostSteps(predictedPostSteps);
            AddSteps(initialPostSteps);
            _postEntries.Add(SourceEntry.CreateSyntheticCompleteJob());
            Render();
        }

        public string JobId { get; }
        public string SourceFileName => _sourceFileName;

        public string Content
        {
            get
            {
                lock (_lock)
                {
                    return _content;
                }
            }
        }

        public int CompleteJobLine
        {
            get
            {
                lock (_lock)
                {
                    return _completeJobLine;
                }
            }
        }

        public int? TryClaimPredictedStep(string matchKey, IStep step)
        {
            if (string.IsNullOrEmpty(matchKey) || step == null)
            {
                return null;
            }

            lock (_lock)
            {
                var existingLine = TryGetLineForStepNoLock(step);
                if (existingLine.HasValue)
                {
                    return existingLine;
                }

                foreach (var entry in _postEntries)
                {
                    if (!string.Equals(entry.MatchKey, matchKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (entry.Step != null && !ReferenceEquals(entry.Step, step))
                    {
                        return null;
                    }

                    entry.Step = step;
                    Render();
                    return TryGetLineForStepNoLock(step);
                }

                return null;
            }
        }

        public int? TryGetLineForStep(IStep step)
        {
            if (step == null)
            {
                return null;
            }

            lock (_lock)
            {
                return TryGetLineForStepNoLock(step);
            }
        }

        private int? TryGetLineForStepNoLock(IStep step)
        {
            foreach (var stepLine in _lineByStep)
            {
                if (ReferenceEquals(stepLine.Step, step))
                {
                    return stepLine.Line;
                }
            }

            return null;
        }

        private void AddSteps(IEnumerable<IStep> steps)
        {
            if (steps == null)
            {
                return;
            }

            foreach (var step in steps)
            {
                if (step == null)
                {
                    continue;
                }

                GetEntries(GetSection(step)).Add(new SourceEntry(step));
            }
        }

        private void AddPredictedPostSteps(IEnumerable<PredictedPostStep> steps)
        {
            if (steps == null)
            {
                return;
            }

            foreach (var step in steps)
            {
                if (step == null)
                {
                    continue;
                }

                _postEntries.Add(new SourceEntry(step.DisplayName, step.MatchKey));
            }
        }

        private List<SourceEntry> GetEntries(SourceSection section)
        {
            switch (section)
            {
                case SourceSection.Pre:
                    return _preEntries;
                case SourceSection.Post:
                    return _postEntries;
                default:
                    return _mainEntries;
            }
        }

        private static SourceSection GetSection(IStep step)
        {
            if (step is IActionRunner actionRunner)
            {
                return GetSection(actionRunner.Stage);
            }

            if (step.ExecutionContext != null)
            {
                return GetSection(step.ExecutionContext.Stage);
            }

            return SourceSection.Main;
        }

        private static SourceSection GetSection(ActionRunStage stage)
        {
            switch (stage)
            {
                case ActionRunStage.Pre:
                    return SourceSection.Pre;
                case ActionRunStage.Post:
                    return SourceSection.Post;
                default:
                    return SourceSection.Main;
            }
        }

        private void Render()
        {
            _lineByStep.Clear();
            _completeJobLine = 0;

            var sb = new StringBuilder();
            var line = 1;

            AppendSection(sb, "pre", _preEntries, ref line, appendSeparatorLine: true);
            AppendSection(sb, "main", _mainEntries, ref line, appendSeparatorLine: true);
            AppendSection(sb, "post", _postEntries, ref line, appendSeparatorLine: false);

            _content = sb.ToString();
        }

        private void AppendSection(
            StringBuilder sb,
            string sectionName,
            IReadOnlyList<SourceEntry> entries,
            ref int line,
            bool appendSeparatorLine)
        {
            sb.Append(sectionName).Append(":\n");
            line++;

            foreach (var entry in entries)
            {
                if (entry.Step != null && TryGetLineForStepNoLock(entry.Step) == null)
                {
                    _lineByStep.Add(new StepLine(entry.Step, line));
                }

                sb.Append("  - step: ");
                sb.Append(FormatYamlString(entry.DisplayName));
                sb.Append('\n');
                if (entry.IsSyntheticCompleteJob)
                {
                    _completeJobLine = line;
                }

                line++;
            }

            if (appendSeparatorLine)
            {
                sb.Append('\n');
                line++;
            }
        }

        private static string FormatYamlString(string value)
        {
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append(@"\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r':
                        sb.Append(@"\r");
                        break;
                    case '\n':
                        sb.Append(@"\n");
                        break;
                    case '\t':
                        sb.Append(@"\t");
                        break;
                    default:
                        if (char.IsControl(c))
                        {
                            sb.Append(@"\u");
                            sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        internal sealed class PredictedPostStep
        {
            public PredictedPostStep(string displayName, string matchKey)
            {
                DisplayName = string.IsNullOrEmpty(displayName) ? "step" : displayName;
                MatchKey = matchKey;
            }

            public string DisplayName { get; }
            public string MatchKey { get; }
        }

        private sealed class StepLine
        {
            public StepLine(IStep step, int line)
            {
                Step = step;
                Line = line;
            }

            public IStep Step { get; }
            public int Line { get; }
        }

        private sealed class SourceEntry
        {
            public SourceEntry(string displayName)
            {
                DisplayName = string.IsNullOrEmpty(displayName) ? "step" : displayName;
            }

            public SourceEntry(string displayName, string matchKey)
                : this(displayName)
            {
                MatchKey = matchKey;
            }

            public SourceEntry(IStep step)
            {
                Step = step;
                DisplayName = string.IsNullOrEmpty(step.DisplayName) ? "step" : step.DisplayName;
            }

            private SourceEntry(string displayName, bool isSyntheticCompleteJob)
                : this(displayName)
            {
                IsSyntheticCompleteJob = isSyntheticCompleteJob;
            }

            public static SourceEntry CreateSyntheticCompleteJob()
            {
                return new SourceEntry("Complete job", isSyntheticCompleteJob: true);
            }

            public IStep Step { get; set; }
            public string DisplayName { get; }
            public string MatchKey { get; }
            public bool IsSyntheticCompleteJob { get; }
        }

        private enum SourceSection
        {
            Pre,
            Main,
            Post
        }
    }
}
