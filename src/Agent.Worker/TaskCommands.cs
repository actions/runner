using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public class TaskCommands : AgentService, ICommandExtension
    {
        public Type ExtensionType
        {
            get
            {
                return typeof(ICommandExtension);
            }
        }

        public String CommandArea
        {
            get
            {
                return "task";
            }
        }

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
            else
            {
                throw new Exception($"##vso[task.{command.Event}] is not a recognized command for Task command extension. TODO: DOC aka link");
            }

            return;
        }

        // Since we process all logging command in serialized order, everthing should be thread safe.
        private readonly Dictionary<Guid, TimelineRecord> _timelineRecordsTracker = new Dictionary<Guid, TimelineRecord>();

        private void ProcessTaskDetailCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            TimelineRecord record = new TimelineRecord();

            String timelineRecord;
            if (!eventProperties.TryGetValue(TaskDetailEventProperties.TimelineRecordId, out timelineRecord) ||
                string.IsNullOrEmpty(timelineRecord) ||
                new Guid(timelineRecord).Equals(Guid.Empty))
            {
                throw new Exception("Can't update timeline record, timeline record id is not provided.");
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
                record.Result = ParseTaskResult(result, TaskResult.Succeeded);
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
                // make sure parentid/order does not changed.
                if (record.Order != null && record.Order != trackingRecord.Order)
                {
                    throw new Exception("Can't change order of an existing timeline record.");
                }

                if (record.ParentId != trackingRecord.ParentId &&
                    record.ParentId != null)
                {
                    throw new Exception("Can't change parent timeline record of an existing timeline record.");
                }
            }
            else
            {
                // we haven't created this timeline record
                // make sure we have name/type/order and parent record has created.
                if (string.IsNullOrEmpty(record.Name))
                {
                    throw new Exception("name is required for this new timeline record.");
                }

                if (string.IsNullOrEmpty(record.RecordType))
                {
                    throw new Exception("type is required for this new timeline record.");
                }

                if (record.Order == null || record.Order < 0)
                {
                    throw new Exception("non-negative order is required for this new timeline record.");
                }

                if (record.ParentId != null && record.ParentId != Guid.Empty)
                {
                    if (!_timelineRecordsTracker.ContainsKey(record.ParentId.Value))
                    {
                        throw new Exception("parent timeline record has not been created for this new timeline record.");
                    }
                }
            }

            context.UpdateDetailTimelineRecord(record);

            _timelineRecordsTracker[record.Id] = record;
        }

        private void ProcessTaskUploadSummaryCommand(IExecutionContext context, string data)
        {
            if (!String.IsNullOrEmpty(data))
            {
                var uploadSummaryProperties = new Dictionary<string, string>();
                uploadSummaryProperties.Add(TaskAddAttachmentEventProperties.Type, CoreAttachmentType.Summary);
                var fileName = Path.GetFileName(data);
                uploadSummaryProperties.Add(TaskAddAttachmentEventProperties.Name, fileName);

                ProcessTaskAddAttachmentCommand(context, uploadSummaryProperties, data);
            }
            else
            {
                throw new Exception("Cannot upload summary file, summary file location is not specified.");
            }
        }

        private void ProcessTaskAddAttachmentCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            String type;
            if (!eventProperties.TryGetValue(TaskAddAttachmentEventProperties.Type, out type) || String.IsNullOrEmpty(type))
            {
                throw new Exception("Can't add task attachment, attachment type is not provided.");
            }

            String name;
            if (!eventProperties.TryGetValue(TaskAddAttachmentEventProperties.Name, out name) || String.IsNullOrEmpty(name))
            {
                throw new Exception("Can't add task attachment, attachment name is not provided.");
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
            if (!String.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                // Upload attachment
                // TODO: add uploadfile to ExecutionContext
                // this.Logger.LogAttachment(this.Scope, type, name, filePath);
            }
            else
            {
                throw new Exception("Cannot upload task attachment file, attachment file location is not specified or attachment file not exist on disk");
            }
        }

        private void ProcessTaskIssueCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            string logLine = "";
            Issue taskIssue = null;

            String issueType;
            if (eventProperties.TryGetValue(TaskIssueEventProperties.Type, out issueType))
            {
                taskIssue = CreateIssue(context, issueType, data, eventProperties, out logLine);
            }

            if (taskIssue == null)
            {
                context.Warning("Can't create TaskIssue from logging event.");
                return;
            }

            context.AddIssue(taskIssue);

            if (!String.IsNullOrEmpty(logLine))
            {
                if (taskIssue.Type == IssueType.Error)
                {
                    context.Write(WellKnownTags.Error, logLine);
                }
                else
                {
                    context.Write(WellKnownTags.Warning, logLine);
                }
            }
        }

        private Issue CreateIssue(IExecutionContext context, string issueType, String message, Dictionary<String, String> properties, out String messageToLog)
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

            messageToLog = message;

            String sourcePath;
            if (properties.TryGetValue(ProjectIssueProperties.SourcePath, out sourcePath))
            {
                issue.Category = "Code";

                var extensionManager = HostContext.GetService<IExtensionManager>();
                string hostType = context.Variables.System_HostType;
                IJobExtension extensions =
                    (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                    .Where(x => string.Equals(x.HostType, hostType, StringComparison.OrdinalIgnoreCase))
                    .First();

                if (extensions != null)
                {
                    // TODO: need source provider in place.
                    // Get the values that represent the server path given a local path
                    //var sourcePathValues = extensions.ConvertLocalPath(null, sourcePath);
                    // replace sourcePath with the new values
                    //foreach (var pair in sourcePathValues)
                    //{
                    //    properties[pair.Key] = pair.Value;
                    //}

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
                    messageToLog = String.Format(CultureInfo.InvariantCulture, "{0}({1},{2}): {3} {4}: {5}",
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

            issue.Message = messageToLog;

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
                throw new Exception("Commond doesn't have valid result value.");
            }

            context.Result = result;
            context.Progress(0, data);
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

        private void ProcessTaskSetVariableCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            String name;
            if (!eventProperties.TryGetValue(TaskSetVariableEventProperties.Variable, out name) || String.IsNullOrEmpty(name))
            {
                return;
            }

            String isSecretValue;
            Boolean isSecret = false;
            if (eventProperties.TryGetValue(TaskSetVariableEventProperties.IsSecret, out isSecretValue))
            {
                Boolean.TryParse(isSecretValue, out isSecret);
            }

            // TODO: SetSecret
            context.Variables.Set(name, data);
        }

        private void ProcessTaskDebugCommand(IExecutionContext context, String data)
        {
            context.Debug(data);
        }

        // Parse String from ##vso command property.
        private TaskResult ParseTaskResult(String resultText, TaskResult defaultValue)
        {
            TaskResult result;
            if (!Enum.TryParse<TaskResult>(resultText, out result))
            {
                result = defaultValue;
            }

            return result;
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

    internal class WellKnownTaskCommand
    {
        public static readonly String AddAttachment = "addattachment";
        public static readonly String Complete = "complete";
        public static readonly String Debug = "debug";
        public static readonly String LogDetail = "logdetail";
        public static readonly String LogIssue = "logissue";
        public static readonly String LogIssue_xplatCompat = "issue";
        public static readonly String SetProgress = "setprogress";
        public static readonly String SetVariable = "setvariable";
        public static readonly String UploadSummary = "uploadsummary";
    }

    internal class TaskProgressEventProperties
    {
        public static readonly String Value = "value";
    }

    internal class TaskSetVariableEventProperties
    {
        public static readonly String Variable = "variable";
        public static readonly String IsSecret = "issecret";
    }

    internal class TaskCompleteEventProperties
    {
        public static readonly String Result = "result";
    }

    internal class TaskIssueEventProperties
    {
        public static readonly String Type = "type";
    }

    internal class ProjectIssueProperties
    {
        public static readonly String Code = "code";
        public static readonly String ColumNumber = "columnnumber";
        public static readonly String SourcePath = "sourcepath";
        public static readonly String LineNumber = "linenumber";
        public static readonly String ProjectId = "id";
    }

    internal class TaskAddAttachmentEventProperties
    {
        public static readonly String Type = "type";
        public static readonly String Name = "name";
    }

    internal class TaskDetailEventProperties
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
}