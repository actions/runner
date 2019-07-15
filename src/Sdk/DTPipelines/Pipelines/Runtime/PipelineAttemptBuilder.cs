using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.Pipelines.Runtime
{
    /// <summary>
    /// Provides functionality to build structured data from the timeline store.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineAttemptBuilder
    {
        public PipelineAttemptBuilder(
            IPipelineIdGenerator idGenerator,
            PipelineProcess pipeline,
            params Timeline[] timelines)
        {
            ArgumentUtility.CheckForNull(idGenerator, nameof(idGenerator));
            ArgumentUtility.CheckForNull(pipeline, nameof(pipeline));

            this.Pipeline = pipeline;
            this.IdGenerator = idGenerator;
            m_recordsById = new Dictionary<Guid, TimelineRecord>();
            m_recordsByParent = new Dictionary<Guid, IList<TimelineRecord>>();
            m_stages = new Dictionary<String, IList<StageAttempt>>(StringComparer.OrdinalIgnoreCase);

            if (timelines?.Length > 0)
            {
                foreach (var timeline in timelines)
                {
                    AddStageAttempts(timeline, m_stages);
                }
            }
        }

        /// <summary>
        /// Gets the ID generator for this pipeline.
        /// </summary>
        public IPipelineIdGenerator IdGenerator
        {
            get;
        }

        /// <summary>
        /// Gets the pipeline document.
        /// </summary>
        public PipelineProcess Pipeline
        {
            get;
        }

        /// <summary>
        /// Creates the initial stage attempts for a brand new pipeline.
        /// </summary>
        /// <returns>A list of initial attempts which should be run</returns>
        public IList<StageAttempt> Initialize()
        {
            var initialAttempts = new List<StageAttempt>();
            foreach (var stage in this.Pipeline.Stages)
            {
                initialAttempts.Add(CreateAttempt(stage));
            }
            return initialAttempts;
        }

        /// <summary>
        /// Produce list of stage attempts needed to retry a pipeline.
        /// By default, we will reuse previously successful stage attempts, and produce new attempts for 
        ///   failed stages, and any stages downstream from a failed stage. 
        /// If specific stage names are provided, only these stages and their descendents will be retried, 
        ///   and will be retried irrespective of previous state.
        /// </summary>
        /// <returns>tuple of all attempts (the full list of attempts to be added to the plan) and "new attempts" (the retries)</returns>
        public Tuple<IList<StageAttempt>, IList<StageAttempt>> Retry(IList<String> stageNames = null)
        {
            var allAttempts = new List<StageAttempt>();
            var newAttempts = new List<StageAttempt>();
            var stagesToRetry = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            GraphValidator.Traverse(this.Pipeline.Stages, (stage, dependencies) =>
            {
                var previousAttempt = GetStageAttempt(stage.Name);
                if (previousAttempt == null)
                {
                    // nothing to retry
                    return;
                }

                // collect some data
                var directlyTargeted = stageNames?.Contains(stage.Name, StringComparer.OrdinalIgnoreCase) is true;
                var needsRetry = NeedsRetry(previousAttempt.Stage.Result);
                var dependencyNeedsRetry = dependencies.Any(x => stagesToRetry.Contains(x));

                // create new attempt
                var newAttempt = default(StageAttempt);
                if (dependencyNeedsRetry
                || (stageNames == default && needsRetry)
                || (stageNames != default && directlyTargeted))
                {
                    // try to create new attempt, if it comes back null, no work needs to be done
                    // force a retry if the stage is directly targeted but the previous attempt was successful. 
                    newAttempt = CreateAttempt(
                        stage,
                        previousAttempt,
                        forceRetry: (directlyTargeted && !needsRetry) || dependencyNeedsRetry);
                }
                
                // update return lists
                if (newAttempt == default)
                {
                    // use previous attempt
                    allAttempts.Add(previousAttempt);
                }
                else
                {
                    stagesToRetry.Add(previousAttempt.Stage.Name);
                    allAttempts.Add(newAttempt);
                    newAttempts.Add(newAttempt);
                }
            });

            return Tuple.Create(
                allAttempts as IList<StageAttempt>,
                newAttempts as IList<StageAttempt>);
        }

        /// <summary>
        /// Create a new stage attempt and a new timeline. 
        /// The new timeline should contain the Pending entries for any stages, phases and jobs that need to be retried.
        /// It should contain a full, re-parented, copy of the timeline subgraphs for stages, phases, and jobs that do not need to be retried. 
        /// </summary>
        private StageAttempt CreateAttempt(
            Stage stage,
            StageAttempt previousStageAttempt = null,
            Boolean forceRetry = false)
        {
            // new instance will have attempt number previous + 1
            var newStageAttempt = new StageAttempt
            {
                Stage = new StageInstance(stage, previousStageAttempt?.Stage.Attempt + 1 ?? 1),
                Timeline = new Timeline(),
            };

            // Compute the stage ID for this attempt
            var stageIdentifier = this.IdGenerator.GetStageIdentifier(newStageAttempt.Stage.Name);
            var stageId = this.IdGenerator.GetStageInstanceId(newStageAttempt.Stage.Name, newStageAttempt.Stage.Attempt);
            newStageAttempt.Timeline.Id = stageId;
            newStageAttempt.Stage.Identifier = stageIdentifier;

            if (previousStageAttempt != null)
            {
                // copy the previous timeline record, reset to "Pending" state
                var previousRecord = m_recordsById[this.IdGenerator.GetStageInstanceId(previousStageAttempt.Stage.Name, previousStageAttempt.Stage.Attempt)];
                newStageAttempt.Timeline.Records.Add(ResetRecord(previousRecord, null, stageId, newStageAttempt.Stage.Attempt));
            }
            else
            {
                // create a new stage record
                newStageAttempt.Timeline.Records.Add(CreateRecord(newStageAttempt.Stage, null, stageId, stage.DisplayName ?? stage.Name, nameof(Stage), m_stageOrder++, stageIdentifier));
            }

            // walk the phases. 
            // if a phase does not need to be retried, copy its entire timeline subgraph to the new timeline. 
            var phaseOrder = 1;
            var phasesRetried = false;
            var phasesToRetry = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            GraphValidator.Traverse(stage.Phases, (phase, dependencies) =>
            {
                var shouldRetry = false;
                var previousPhaseAttempt = previousStageAttempt?.Phases.FirstOrDefault(x => String.Equals(x.Phase.Name, phase.Name, StringComparison.OrdinalIgnoreCase));
                var upstreamDependencyNeedsRetry = dependencies.Any(x => phasesToRetry.Contains(x));
                var previousAttemptNeedsRetry = NeedsRetry(previousPhaseAttempt?.Phase.Result);
                if (forceRetry || upstreamDependencyNeedsRetry || previousAttemptNeedsRetry)
                {
                    // If the previous attempt a specific phase failed then we should retry it and everything
                    // downstream regardless of first attempt status. The failed phases are appended as we walk
                    // the graph and the set is inspected 
                    shouldRetry = true;
                    phasesToRetry.Add(phase.Name);
                }

                if (!shouldRetry && previousPhaseAttempt != null)
                {
                    // This phase does not need to be retried. 
                    // Copy timeline records from previous timeline to new timeline. 
                    // The new timeline should report that this phase has already been run, and the parent should be the new stage. 
                    previousPhaseAttempt.Phase.Definition = phase;
                    newStageAttempt.Phases.Add(previousPhaseAttempt);

                    // clone so as not to mess up our lookup table. 
                    var previousPhaseId = this.IdGenerator.GetPhaseInstanceId(newStageAttempt.Stage.Name, previousPhaseAttempt.Phase.Name, previousPhaseAttempt.Phase.Attempt);
                    var newPhaseRecord = m_recordsById[previousPhaseId].Clone();
                    newPhaseRecord.ParentId = stageId; // this phase is already completed for the new stage. 

                    phaseOrder = (newPhaseRecord.Order ?? phaseOrder) + 1; // TODO: what does this do? 
                    newStageAttempt.Timeline.Records.Add(newPhaseRecord);

                    // if there are any child records of the phase, copy them too. 
                    // they should exist exactly as they are on the new timeline. 
                    // Only the phase needs to reparent. 
                    newStageAttempt.Timeline.Records.AddRange(CollectAllChildren(newPhaseRecord));
                }
                else
                {
                    // This phase needs to be retried. 
                    // Track that we are scheduling a phase for retry in this attempt
                    phasesRetried = true;

                    // Create a new attempt record in the pending state. At runtime the job expansion logic, based on the target
                    // strategy, will determine what needs to be re-run and what doesn't based on the previous attempt. We don't
                    // make assumptions about the internals of jobs here as that is the piece the orchestrator doesn't deal with
                    // directly.
                    var newPhaseAttempt = new PhaseAttempt
                    {
                        Phase = new PhaseInstance(phase, previousPhaseAttempt?.Phase.Attempt + 1 ?? 1),
                    };

                    var phaseId = this.IdGenerator.GetPhaseInstanceId(
                        newStageAttempt.Stage.Name, 
                        newPhaseAttempt.Phase.Name, 
                        newPhaseAttempt.Phase.Attempt);

                    newPhaseAttempt.Phase.Identifier = this.IdGenerator.GetPhaseIdentifier(newStageAttempt.Stage.Name, newPhaseAttempt.Phase.Name);
                    newStageAttempt.Timeline.Records.Add(CreateRecord(
                        newPhaseAttempt.Phase, 
                        stageId, 
                        phaseId, 
                        phase.DisplayName ?? phase.Name, 
                        nameof(Phase), 
                        phaseOrder++, 
                        newPhaseAttempt.Phase.Identifier));

                    // The previous attempt failed but we had no upstream failures means that this specific phase 
                    // needs have the failed jobs re-run. 
                    // For this case we just locate the failed jobs and create new
                    // attempt records to ensure they are re-run.
                    if (previousAttemptNeedsRetry && !upstreamDependencyNeedsRetry)
                    {
                        foreach (var previousJobAttempt in previousPhaseAttempt.Jobs)
                        {
                            var previousJobId = this.IdGenerator.GetJobInstanceId(
                                newStageAttempt.Stage.Name, 
                                newPhaseAttempt.Phase.Name, 
                                previousJobAttempt.Job.Name, 
                                previousJobAttempt.Job.Attempt);

                            if (NeedsRetry(previousJobAttempt.Job.Result))
                            {
                                // this job needs to be retried. 
                                //
                                // NOTE: 
                                // Phases (JobFactories) normally are expanded dynamically to produce jobs. 
                                // The phase expansion routines allow a list of configurations to be supplied. If non-empty, the JobFactories will only 
                                //   produce Jobs with the names provided.
                                // 
                                // In retry attempts, we already know the Job names that will be produced, and we only want to run a subset of them. 
                                // We can define the subset of jobs to be "expanded" by initializing the PhaseAttempt with named JobAttempts. 
                                // See RunPhase for more details.
                                var newJobAttempt = new JobAttempt
                                {
                                    Job = new JobInstance(previousJobAttempt.Job.Name, previousJobAttempt.Job.Attempt + 1),
                                };
                                newJobAttempt.Job.Identifier = this.IdGenerator.GetJobIdentifier(
                                    newStageAttempt.Stage.Name, 
                                    newPhaseAttempt.Phase.Name, 
                                    newJobAttempt.Job.Name);
                                newPhaseAttempt.Jobs.Add(newJobAttempt);

                                // create a new record in "Pending" state based on the previous record. 
                                var newJobId = this.IdGenerator.GetJobInstanceId(
                                    newStageAttempt.Stage.Name, 
                                    newPhaseAttempt.Phase.Name, 
                                    newJobAttempt.Job.Name, 
                                    newJobAttempt.Job.Attempt);
                                newStageAttempt.Timeline.Records.Add(ResetRecord(m_recordsById[previousJobId], phaseId, newJobId, newJobAttempt.Job.Attempt));
                            }
                            else
                            {
                                // this job does not need to be retried. 
                                // deep copy the timeline subgraph to the new timeline.
                                // reparent the job record to the new phase id so the job looks completed on the new timeline. 
                                var newJobRecord = m_recordsById[previousJobId].Clone();
                                newJobRecord.ParentId = phaseId;
                                newPhaseAttempt.Jobs.Add(previousJobAttempt);
                                newStageAttempt.Timeline.Records.Add(newJobRecord);
                                newStageAttempt.Timeline.Records.AddRange(CollectAllChildren(newJobRecord));
                            }
                        }
                    }

                    newStageAttempt.Phases.Add(newPhaseAttempt);
                }
            });

            if (!phasesRetried)
            {
                // The stage will remain complete so there is no reason to register a new attempt
                return null;
            }

            // If this is a new pipeline store that is empty we need to initialize the attempts for this stage.
            if (!m_stages.TryGetValue(stage.Name, out IList<StageAttempt> attempts))
            {
                attempts = new List<StageAttempt>();
                m_stages[stage.Name] = attempts;
            }

            attempts.Add(newStageAttempt);
            return newStageAttempt;
        }

        public StageAttempt GetStageAttempt(
            String name,
            Int32 attempt = -1)
        {
            if (!m_stages.TryGetValue(name, out var attempts))
            {
                return null;
            }

            if (attempt <= 0)
            {
                return attempts.OrderByDescending(x => x.Stage.Attempt).FirstOrDefault();
            }
            else
            {
                return attempts.FirstOrDefault(x => x.Stage.Attempt == attempt);
            }
        }

        /// <summary>
        /// returns true if result should be retried.
        /// </summary>
        internal static Boolean NeedsRetry(TaskResult? result)
        {
            return result == TaskResult.Abandoned
                || result == TaskResult.Canceled
                || result == TaskResult.Failed;
        }

        private TimelineRecord CreateRecord(
            IGraphNodeInstance node,
            Guid? parentId,
            Guid recordId,
            String name,
            String type,
            Int32 order,
            String identifier)
        {
            return new TimelineRecord
            {
                Attempt = node.Attempt,
                Id = recordId,
                Identifier = identifier,
                Name = name,
                Order = order,
                ParentId = parentId,
                RecordType = type,
                RefName = node.Name,
                State = TimelineRecordState.Pending,
            };
        }

        /// <summary>
        /// creates a new timeline record with Pending state based on the input. 
        /// </summary>
        private TimelineRecord ResetRecord(
            TimelineRecord record,
            Guid? parentId,
            Guid newId,
            Int32 attempt)
        {
            return new TimelineRecord
            {
                // new stuff
                Attempt = attempt,
                Id = newId,
                ParentId = parentId,
                State = TimelineRecordState.Pending,

                // old stuff
                Identifier = record.Identifier,
                Name = record.Name,
                Order = record.Order,
                RecordType = record.RecordType,
                RefName = record.RefName,
            };
        }

        /// <summary>
        /// Returns tuple of recordsById, recordsByParentId
        /// </summary>
        internal static Tuple<IDictionary<Guid, TimelineRecord>, IDictionary<Guid, IList<TimelineRecord>>> ParseTimeline(Timeline timeline)
        {
            var recordsById = new Dictionary<Guid, TimelineRecord>();
            var recordsByParentId = new Dictionary<Guid, IList<TimelineRecord>>();

            foreach (var record in timeline?.Records)
            {
                recordsById[record.Id] = record;

                if (record.ParentId != null)
                {
                    if (!recordsByParentId.TryGetValue(record.ParentId.Value, out var childRecords))
                    {
                        childRecords = new List<TimelineRecord>();
                        recordsByParentId.Add(record.ParentId.Value, childRecords);
                    }

                    childRecords.Add(record);
                }
                else if (record.RecordType == nameof(Stage))
                {
                    FixRecord(record);
                }
            }

            return Tuple.Create(
                recordsById as IDictionary<Guid, TimelineRecord>, 
                recordsByParentId as IDictionary<Guid, IList<TimelineRecord>>);
        }

        private void AddStageAttempts(
            Timeline timeline,
            IDictionary<String, IList<StageAttempt>> attempts)
        {
            if (timeline == default)
            {
                return; // nothing to do
            }

            // parse timeline
            var tuple = ParseTimeline(timeline);
            m_recordsById = tuple.Item1;
            m_recordsByParent = tuple.Item2;

            foreach (var stageRecord in m_recordsById.Values.Where(x => x.RecordType == "Stage"))
            {
                var attempt = new StageAttempt
                {
                    Stage = new StageInstance
                    {
                        Attempt = stageRecord.Attempt,
                        FinishTime = stageRecord.FinishTime,
                        Identifier = stageRecord.Identifier,
                        Name = stageRecord.RefName,
                        Result = stageRecord.Result,
                        StartTime = stageRecord.StartTime,
                        State = Convert(stageRecord.State.Value),
                    },
                    Timeline = new Timeline
                    {
                        Id = timeline.Id,
                    },
                };

                attempt.Timeline.Records.Add(stageRecord);

                if (m_recordsByParent.TryGetValue(stageRecord.Id, out var phaseRecords))
                {
                    AddPhaseAttempts(
                        attempt,
                        phaseRecords.Where(x => x.RecordType == nameof(Phase)),
                        m_recordsByParent);
                }

                if (!attempts.TryGetValue(attempt.Stage.Identifier, out var stageAttempts))
                {
                    stageAttempts = new List<StageAttempt>();
                    attempts.Add(attempt.Stage.Identifier, stageAttempts);
                }

                stageAttempts.Add(attempt);
            }
        }

        private void AddPhaseAttempts(
            StageAttempt stageAttempt,
            IEnumerable<TimelineRecord> phaseRecords,
            IDictionary<Guid, IList<TimelineRecord>> recordsByParent)
        {
            foreach (var phaseRecord in phaseRecords)
            {
                FixRecord(phaseRecord);

                var phaseAttempt = new PhaseAttempt
                {
                    Phase = new PhaseInstance
                    {
                        Attempt = phaseRecord.Attempt,
                        FinishTime = phaseRecord.FinishTime,
                        Identifier = phaseRecord.Identifier,
                        Name = phaseRecord.RefName,
                        Result = phaseRecord.Result,
                        StartTime = phaseRecord.StartTime,
                        State = Convert(phaseRecord.State.Value),
                    },
                };

                stageAttempt.Phases.Add(phaseAttempt);
                stageAttempt.Timeline.Records.Add(phaseRecord);

                // Drive down into the individual jobs if they exist
                if (recordsByParent.TryGetValue(phaseRecord.Id, out var jobRecords))
                {
                    AddJobAttempts(
                        stageAttempt,
                        phaseAttempt,
                        jobRecords.Where(x => x.RecordType == nameof(Job)),
                        recordsByParent);
                }
            }
        }

        private void AddJobAttempts(
            StageAttempt stageAttempt,
            PhaseAttempt phaseAttempt,
            IEnumerable<TimelineRecord> jobRecords,
            IDictionary<Guid, IList<TimelineRecord>> recordsByParent)
        {
            foreach (var jobRecord in jobRecords)
            {
                FixRecord(jobRecord);

                var jobAttempt = new JobAttempt
                {
                    Job = new JobInstance
                    {
                        Attempt = jobRecord.Attempt,
                        FinishTime = jobRecord.FinishTime,
                        Identifier = jobRecord.Identifier,
                        Name = jobRecord.RefName,
                        Result = jobRecord.Result,
                        StartTime = jobRecord.StartTime,
                        State = Convert(jobRecord.State.Value),
                    },
                };

                phaseAttempt.Jobs.Add(jobAttempt);
                stageAttempt.Timeline.Records.Add(jobRecord);

                // Just blindly copy the child records
                stageAttempt.Timeline.Records.AddRange(CollectAllChildren(jobRecord));
            }
        }

        internal IList<TimelineRecord> CollectAllChildren(
            TimelineRecord root,
            Int32 maxDepth = int.MaxValue)
        {
            return CollectAllChildren(root, m_recordsByParent, maxDepth);
        }

        internal static IList<TimelineRecord> CollectAllChildren(
            TimelineRecord root,
            IDictionary<Guid, IList<TimelineRecord>> recordsByParent,
            Int32 maxDepth = int.MaxValue)
        {
            var result = new List<TimelineRecord>();
            if (!recordsByParent.TryGetValue(root.Id, out var childRecords))
            {
                return result;
            }

            // instead of actually recursing, create a queue of record, depth pairs. 
            var recordQueue = new Queue<Tuple<TimelineRecord, int>>(childRecords.Select(x => Tuple.Create(x, 1)));
            while (recordQueue.Count > 0)
            {
                var t = recordQueue.Dequeue();
                var currentRecord = t.Item1;
                var currentDepth = t.Item2;

                // collect record
                result.Add(currentRecord);

                // check depth
                if (currentDepth >= maxDepth)
                {
                    continue;
                }

                // enqueue children
                var childDepth = currentDepth + 1;
                if (recordsByParent.TryGetValue(currentRecord.Id, out var newChildren))
                {
                    foreach (var newChild in newChildren)
                    {
                        recordQueue.Enqueue(Tuple.Create(newChild, childDepth));
                    }
                }
            }

            return result;
        }

        private static PipelineState Convert(TimelineRecordState state)
        {
            switch (state)
            {
                case TimelineRecordState.Completed:
                    return PipelineState.Completed;
                case TimelineRecordState.InProgress:
                    return PipelineState.InProgress;
            }

            return PipelineState.NotStarted;
        }

        /// <summary>
        /// The timeline records get normalized into strings which are not case-sensitive, meaning the input
        ///   casing may not match what is output.In order to compensate for this we update the ref name from
        ///   the identifier, as the identifier reflects the actual value.
        /// </summary>
        private static void FixRecord(TimelineRecord record)
        {
            if (!String.IsNullOrEmpty(record.Identifier))
            {
                record.RefName = PipelineUtilities.GetName(record.Identifier);
            }
        }

        // Includes all attempts of all stages
        private Int32 m_stageOrder = 1;
        private IDictionary<Guid, TimelineRecord> m_recordsById;
        private IDictionary<Guid, IList<TimelineRecord>> m_recordsByParent;
        private IDictionary<String, IList<StageAttempt>> m_stages;
    }
}
