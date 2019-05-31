﻿using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class TaskCommandExtension : AgentService, IWorkerCommandExtension
    {
        // Since we process all logging command in serialized order, everthing should be thread safe.
        private readonly Dictionary<Guid, TimelineRecord> _timelineRecordsTracker = new Dictionary<Guid, TimelineRecord>();

        public Type ExtensionType => typeof(IWorkerCommandExtension);

        public string CommandArea => "task";

        public HostTypes SupportedHostTypes => HostTypes.All;

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            // TODO: update tasklib alway product ##vso[task.logissue]
            if (String.Equals(command.Event, WellKnownTaskCommand.LogIssue, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(command.Event, WellKnownTaskCommand.LogIssue_xplatCompat, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskIssueCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.SetProgress, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskProgressCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.LogDetail, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskDetailCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.Complete, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskCompleteCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.SetSecret, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskSetSecretCommand(context, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.SetVariable, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskSetVariableCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.AddAttachment, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskAddAttachmentCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.Debug, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskDebugCommand(context, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.UploadSummary, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskUploadSummaryCommand(context, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.UploadFile, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskUploadFileCommand(context, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.SetTaskVariable, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskSetTaskVariableCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.SetEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskSetEndpointCommand(context, command.Properties, command.Data);
            }
            else if (String.Equals(command.Event, WellKnownTaskCommand.PrependPath, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTaskPrepandPathCommand(context, command.Data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("TaskCommandNotFound", command.Event));
            }
        }

        private void ProcessTaskDetailCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            TimelineRecord record = new TimelineRecord();

            String timelineRecord;
            if (!eventProperties.TryGetValue(TaskDetailEventProperties.TimelineRecordId, out timelineRecord) ||
                string.IsNullOrEmpty(timelineRecord) ||
                new Guid(timelineRecord).Equals(Guid.Empty))
            {
                throw new Exception(StringUtil.Loc("MissingTimelineRecordId"));
            }
            else
            {
                record.Id = new Guid(timelineRecord);
            }

            string parentTimlineRecord;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.ParentTimelineRecordId, out parentTimlineRecord))
            {
                record.ParentId = new Guid(parentTimlineRecord);
            }

            String name;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.Name, out name))
            {
                record.Name = name;
            }

            String recordType;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.Type, out recordType))
            {
                record.RecordType = recordType;
            }

            String order;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.Order, out order))
            {
                int orderInt = 0;
                if (int.TryParse(order, out orderInt))
                {
                    record.Order = orderInt;
                }
            }

            String percentCompleteValue;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.Progress, out percentCompleteValue))
            {
                Int32 progress;
                if (Int32.TryParse(percentCompleteValue, out progress))
                {
                    record.PercentComplete = (Int32)Math.Min(Math.Max(progress, 0), 100);
                }
            }

            if (!String.IsNullOrEmpty(data))
            {
                record.CurrentOperation = data;
            }

            string result;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.Result, out result))
            {
                record.Result = EnumUtil.TryParse<TaskResult>(result) ?? TaskResult.Succeeded;
            }

            String startTime;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.StartTime, out startTime))
            {
                record.StartTime = ParseDateTime(startTime, DateTime.UtcNow);
            }

            String finishtime;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.FinishTime, out finishtime))
            {
                record.FinishTime = ParseDateTime(finishtime, DateTime.UtcNow);
            }

            String state;
            if (eventProperties.TryGetValue(TaskDetailEventProperties.State, out state))
            {
                record.State = ParseTimelineRecordState(state, TimelineRecordState.Pending);
            }


            TimelineRecord trackingRecord;
            // in front validation as much as possible.
            // timeline record is happened in back end queue, user will not receive result of the timeline record updates.
            // front validation will provide user better understanding when things went wrong.
            if (_timelineRecordsTracker.TryGetValue(record.Id, out trackingRecord))
            {
                // we already created this timeline record
                // make sure parentid does not changed.
                if (record.ParentId != null &&
                    record.ParentId != trackingRecord.ParentId)
                {
                    throw new Exception(StringUtil.Loc("CannotChangeParentTimelineRecord"));
                }
                else if (record.ParentId == null)
                {
                    record.ParentId = trackingRecord.ParentId;
                }

                // populate default value for empty field.
                if (record.State == TimelineRecordState.Completed)
                {
                    if (record.PercentComplete == null)
                    {
                        record.PercentComplete = 100;
                    }

                    if (record.FinishTime == null)
                    {
                        record.FinishTime = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                // we haven't created this timeline record
                // make sure we have name/type and parent record has created.
                if (string.IsNullOrEmpty(record.Name))
                {
                    throw new Exception(StringUtil.Loc("NameRequiredForTimelineRecord"));
                }

                if (string.IsNullOrEmpty(record.RecordType))
                {
                    throw new Exception(StringUtil.Loc("TypeRequiredForTimelineRecord"));
                }

                if (record.ParentId != null && record.ParentId != Guid.Empty)
                {
                    if (!_timelineRecordsTracker.ContainsKey(record.ParentId.Value))
                    {
                        throw new Exception(StringUtil.Loc("ParentTimelineNotCreated"));
                    }
                }

                // populate default value for empty field.
                if (record.StartTime == null)
                {
                    record.StartTime = DateTime.UtcNow;
                }

                if (record.State == null)
                {
                    record.State = TimelineRecordState.InProgress;
                }
            }

            context.UpdateDetailTimelineRecord(record);

            _timelineRecordsTracker[record.Id] = record;
        }

        private void ProcessTaskUploadSummaryCommand(IExecutionContext context, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                var uploadSummaryProperties = new Dictionary<string, string>();
                uploadSummaryProperties.Add(TaskAddAttachmentEventProperties.Type, CoreAttachmentType.Summary);
                var fileName = Path.GetFileName(data);
                uploadSummaryProperties.Add(TaskAddAttachmentEventProperties.Name, fileName);

                ProcessTaskAddAttachmentCommand(context, uploadSummaryProperties, data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("CannotUploadSummary"));
            }
        }

        private void ProcessTaskUploadFileCommand(IExecutionContext context, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                var uploadFileProperties = new Dictionary<string, string>();
                uploadFileProperties.Add(TaskAddAttachmentEventProperties.Type, CoreAttachmentType.FileAttachment);
                var fileName = Path.GetFileName(data);
                uploadFileProperties.Add(TaskAddAttachmentEventProperties.Name, fileName);

                ProcessTaskAddAttachmentCommand(context, uploadFileProperties, data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("CannotUploadFile"));
            }
        }

        private void ProcessTaskAddAttachmentCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            String type;
            if (!eventProperties.TryGetValue(TaskAddAttachmentEventProperties.Type, out type) || String.IsNullOrEmpty(type))
            {
                throw new Exception(StringUtil.Loc("MissingAttachmentType"));
            }

            String name;
            if (!eventProperties.TryGetValue(TaskAddAttachmentEventProperties.Name, out name) || String.IsNullOrEmpty(name))
            {
                throw new Exception(StringUtil.Loc("MissingAttachmentName"));
            }

            char[] s_invalidFileChars = Path.GetInvalidFileNameChars();
            if (type.IndexOfAny(s_invalidFileChars) != -1)
            {
                throw new Exception($"Type contain invalid characters. ({String.Join(",", s_invalidFileChars)})");
            }

            if (name.IndexOfAny(s_invalidFileChars) != -1)
            {
                throw new Exception($"Name contain invalid characters. ({String.Join(", ", s_invalidFileChars)})");
            }

            string filePath = data;
            if (context.Container != null)
            {
                // Translate file path back from container path
                filePath = context.Container.TranslateToHostPath(filePath);
            }

            if (!String.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                // Upload attachment
                context.QueueAttachFile(type, name, filePath);
            }
            else
            {
                throw new Exception(StringUtil.Loc("MissingAttachmentFile"));
            }
        }

        private void ProcessTaskIssueCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            Issue taskIssue = null;

            String issueType;
            if (eventProperties.TryGetValue(TaskIssueEventProperties.Type, out issueType))
            {
                taskIssue = CreateIssue(context, issueType, data, eventProperties);
            }

            if (taskIssue == null)
            {
                context.Warning("Can't create TaskIssue from logging event.");
                return;
            }

            context.AddIssue(taskIssue);
        }

        private Issue CreateIssue(IExecutionContext context, string issueType, String message, Dictionary<String, String> properties)
        {
            Issue issue = new Issue()
            {
                Category = "General",
            };

            if (issueType.Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                issue.Type = IssueType.Error;
            }
            else if (issueType.Equals("warning", StringComparison.OrdinalIgnoreCase))
            {
                issue.Type = IssueType.Warning;
            }
            else
            {
                throw new Exception($"issue type {issueType} is not an expected issue type.");
            }

            String sourcePath;
            if (properties.TryGetValue(ProjectIssueProperties.SourcePath, out sourcePath))
            {
                issue.Category = "Code";

                var extensionManager = HostContext.GetService<IExtensionManager>();
                var hostType = context.Variables.System_HostType;
                IJobExtension extension =
                    (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                    .Where(x => x.HostType.HasFlag(hostType))
                    .FirstOrDefault();

                if (extension != null)
                {
                    if (context.Container != null)
                    {
                        // Translate file path back from container path
                        sourcePath = context.Container.TranslateToHostPath(sourcePath);
                        properties[ProjectIssueProperties.SourcePath] = sourcePath;
                    }

                    // Get the values that represent the server path given a local path
                    string repoName;
                    string relativeSourcePath;
                    extension.ConvertLocalPath(context, sourcePath, out repoName, out relativeSourcePath);

                    // add repo info
                    if (!string.IsNullOrEmpty(repoName))
                    {
                        properties["repo"] = repoName;
                    }

                    if (!string.IsNullOrEmpty(relativeSourcePath))
                    {
                        // replace sourcePath with the new relative path
                        properties[ProjectIssueProperties.SourcePath] = relativeSourcePath;
                    }

                    String sourcePathValue;
                    String lineNumberValue;
                    String columnNumberValue;
                    String messageTypeValue;
                    String codeValue;
                    properties.TryGetValue(TaskIssueEventProperties.Type, out messageTypeValue);
                    properties.TryGetValue(ProjectIssueProperties.SourcePath, out sourcePathValue);
                    properties.TryGetValue(ProjectIssueProperties.LineNumber, out lineNumberValue);
                    properties.TryGetValue(ProjectIssueProperties.ColumNumber, out columnNumberValue);
                    properties.TryGetValue(ProjectIssueProperties.Code, out codeValue);

                    //ex. Program.cs(13, 18): error CS1002: ; expected
                    message = String.Format(CultureInfo.InvariantCulture, "{0}({1},{2}): {3} {4}: {5}",
                        sourcePathValue,
                        lineNumberValue,
                        columnNumberValue,
                        messageTypeValue,
                        codeValue,
                        message);
                }
            }

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    issue.Data[property.Key] = property.Value;
                }
            }

            issue.Message = message;

            return issue;
        }

        private void ProcessTaskCompleteCommand(IExecutionContext context, Dictionary<string, string> eventProperties, String data)
        {
            string resultText;
            TaskResult result;
            if (!eventProperties.TryGetValue(TaskCompleteEventProperties.Result, out resultText) ||
                String.IsNullOrEmpty(resultText) ||
                !Enum.TryParse<TaskResult>(resultText, out result))
            {
                throw new Exception(StringUtil.Loc("InvalidCommandResult"));
            }

            context.Result = TaskResultUtil.MergeTaskResults(context.Result, result);
            context.Progress(100, data);

            if (eventProperties.TryGetValue(TaskCompleteEventProperties.Done, out string doneText) &&
                !String.IsNullOrEmpty(doneText) &&
                StringUtil.ConvertToBoolean(doneText))
            {
                context.ForceTaskComplete();
            }
        }

        private void ProcessTaskProgressCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            Int32 percentComplete = 0;
            String processValue;
            if (eventProperties.TryGetValue("value", out processValue))
            {
                Int32 progress;
                if (Int32.TryParse(processValue, out progress))
                {
                    percentComplete = (Int32)Math.Min(Math.Max(progress, 0), 100);
                }
            }

            context.Progress(percentComplete, data);
        }

        private void ProcessTaskSetSecretCommand(IExecutionContext context, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                HostContext.SecretMasker.AddValue(data);
            }
        }

        private void ProcessTaskSetVariableCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            String name;
            if (!eventProperties.TryGetValue(TaskSetVariableEventProperties.Variable, out name) || String.IsNullOrEmpty(name))
            {
                throw new Exception(StringUtil.Loc("MissingVariableName"));
            }

            String isSecretValue;
            Boolean isSecret = false;
            if (eventProperties.TryGetValue(TaskSetVariableEventProperties.IsSecret, out isSecretValue))
            {
                Boolean.TryParse(isSecretValue, out isSecret);
            }

            String isOutputValue;
            Boolean isOutput = false;
            if (eventProperties.TryGetValue(TaskSetVariableEventProperties.IsOutput, out isOutputValue))
            {
                Boolean.TryParse(isOutputValue, out isOutput);
            }

            if (isSecret)
            {
                bool? allowMultilineSecret = context.Variables.GetBoolean("SYSTEM_UNSAFEALLOWMULTILINESECRET");
                if (allowMultilineSecret == null)
                {
                    allowMultilineSecret = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("SYSTEM_UNSAFEALLOWMULTILINESECRET"), false);
                }

                if (!string.IsNullOrEmpty(data) &&
                    data.Contains(Environment.NewLine) &&
                    !allowMultilineSecret.Value)
                {
                    throw new InvalidOperationException(StringUtil.Loc("MultilineSecret"));
                }
            }

            context.SetVariable(name, data, isSecret, isOutput);
        }

        private void ProcessTaskDebugCommand(IExecutionContext context, String data)
        {
            context.Debug(data);
        }

        private void ProcessTaskSetTaskVariableCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            String name;
            if (!eventProperties.TryGetValue(TaskSetTaskVariableEventProperties.Variable, out name) || String.IsNullOrEmpty(name))
            {
                throw new Exception(StringUtil.Loc("MissingTaskVariableName"));
            }

            String isSecretValue;
            Boolean isSecret = false;
            if (eventProperties.TryGetValue(TaskSetTaskVariableEventProperties.IsSecret, out isSecretValue))
            {
                Boolean.TryParse(isSecretValue, out isSecret);
            }

            if (isSecret)
            {
                bool? allowMultilineSecret = context.Variables.GetBoolean("SYSTEM_UNSAFEALLOWMULTILINESECRET");
                if (allowMultilineSecret == null)
                {
                    allowMultilineSecret = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("SYSTEM_UNSAFEALLOWMULTILINESECRET"), false);
                }

                if (!string.IsNullOrEmpty(data) &&
                    data.Contains(Environment.NewLine) &&
                    !allowMultilineSecret.Value)
                {
                    throw new InvalidOperationException(StringUtil.Loc("MultilineSecret"));
                }
            }

            // context.TaskVariables.Set(name, data, isSecret);
        }

        private void ProcessTaskSetEndpointCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new Exception(StringUtil.Loc("EnterValidValueFor0", "setendpoint"));
            }

            String field;
            if (!eventProperties.TryGetValue(TaskSetEndpointEventProperties.Field, out field) || String.IsNullOrEmpty(field))
            {
                throw new Exception(StringUtil.Loc("MissingEndpointField"));
            }

            // Mask auth parameter data upfront to avoid accidental secret exposure by invalid endpoint/key/data 
            if (String.Equals(field, "authParameter", StringComparison.OrdinalIgnoreCase))
            {
                HostContext.SecretMasker.AddValue(data);
            }

            String endpointIdInput;
            if (!eventProperties.TryGetValue(TaskSetEndpointEventProperties.EndpointId, out endpointIdInput) || String.IsNullOrEmpty(endpointIdInput))
            {
                throw new Exception(StringUtil.Loc("MissingEndpointId"));
            }

            Guid endpointId;
            if (!Guid.TryParse(endpointIdInput, out endpointId))
            {
                throw new Exception(StringUtil.Loc("InvalidEndpointId"));
            }

            var endpoint = context.Endpoints.Find(a => a.Id == endpointId);
            if (endpoint == null)
            {
                throw new Exception(StringUtil.Loc("InvalidEndpointId"));
            }

            if (String.Equals(field, "url", StringComparison.OrdinalIgnoreCase))
            {
                Uri uri;
                if (!Uri.TryCreate(data, UriKind.Absolute, out uri))
                {
                    throw new Exception(StringUtil.Loc("InvalidEndpointUrl"));
                }

                endpoint.Url = uri;
                return;
            }

            String key;
            if (!eventProperties.TryGetValue(TaskSetEndpointEventProperties.Key, out key) || String.IsNullOrEmpty(key))
            {
                throw new Exception(StringUtil.Loc("MissingEndpointKey"));
            }

            if (String.Equals(field, "dataParameter", StringComparison.OrdinalIgnoreCase))
            {
                endpoint.Data[key] = data;
            }
            else if (String.Equals(field, "authParameter", StringComparison.OrdinalIgnoreCase))
            {
                endpoint.Authorization.Parameters[key] = data;
            }
            else
            {
                throw new Exception(StringUtil.Loc("InvalidEndpointField"));
            }
        }

        private void ProcessTaskPrepandPathCommand(IExecutionContext context, string data)
        {
            ArgUtil.NotNullOrEmpty(data, nameof(WellKnownTaskCommand.PrependPath));
            context.PrependPath.RemoveAll(x => string.Equals(x, data, StringComparison.CurrentCulture));
            context.PrependPath.Add(data);
        }

        private DateTime ParseDateTime(String dateTimeText, DateTime defaultValue)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(dateTimeText, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                dateTime = defaultValue;
            }

            return dateTime;
        }

        private TimelineRecordState ParseTimelineRecordState(String timelineRecordStateText, TimelineRecordState defaultValue)
        {
            TimelineRecordState state;
            if (!Enum.TryParse<TimelineRecordState>(timelineRecordStateText, out state))
            {
                state = defaultValue;
            }

            return state;
        }
    }

    internal static class WellKnownTaskCommand
    {
        public static readonly String AddAttachment = "addattachment";
        public static readonly String Complete = "complete";
        public static readonly String Debug = "debug";
        public static readonly String LogDetail = "logdetail";
        public static readonly String LogIssue = "logissue";
        public static readonly String LogIssue_xplatCompat = "issue";
        public static readonly String PrependPath = "prependpath";
        public static readonly String SetProgress = "setprogress";
        public static readonly String SetSecret = "setsecret";
        public static readonly String SetVariable = "setvariable";
        public static readonly String SetTaskVariable = "settaskvariable";
        public static readonly String SetEndpoint = "setendpoint";
        public static readonly String UploadFile = "uploadfile";
        public static readonly String UploadSummary = "uploadsummary";
    }

    internal static class TaskProgressEventProperties
    {
        public static readonly String Value = "value";
    }

    internal static class TaskSetVariableEventProperties
    {
        public static readonly String Variable = "variable";
        public static readonly String IsSecret = "issecret";
        public static readonly String IsOutput = "isoutput";
    }

    internal static class TaskCompleteEventProperties
    {
        public static readonly String Result = "result";
        public static readonly String Done = "done";
    }

    internal static class TaskIssueEventProperties
    {
        public static readonly String Type = "type";
    }

    internal static class ProjectIssueProperties
    {
        public static readonly String Code = "code";
        public static readonly String ColumNumber = "columnnumber";
        public static readonly String SourcePath = "sourcepath";
        public static readonly String LineNumber = "linenumber";
        public static readonly String ProjectId = "id";
    }

    internal static class TaskAddAttachmentEventProperties
    {
        public static readonly String Type = "type";
        public static readonly String Name = "name";
    }

    internal static class TaskDetailEventProperties
    {
        public static readonly String TimelineRecordId = "id";
        public static readonly String ParentTimelineRecordId = "parentid";
        public static readonly String Type = "type";
        public static readonly String Name = "name";
        public static readonly String StartTime = "starttime";
        public static readonly String FinishTime = "finishtime";
        public static readonly String Progress = "progress";
        public static readonly String State = "state";
        public static readonly String Result = "result";
        public static readonly String Order = "order";
    }

    internal static class TaskSetTaskVariableEventProperties
    {
        public static readonly String Variable = "variable";
        public static readonly String IsSecret = "issecret";
    }

    internal static class TaskSetEndpointEventProperties
    {
        public static readonly String EndpointId = "id";
        public static readonly String Field = "field";
        public static readonly String Key = "key";
    }
}