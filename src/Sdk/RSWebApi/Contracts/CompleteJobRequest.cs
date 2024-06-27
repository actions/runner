﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;
using Sdk.RSWebApi.Contracts;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class CompleteJobRequest
    {
        [DataMember(Name = "planId", EmitDefaultValue = false)]
        public Guid PlanID { get; set; }

        [DataMember(Name = "jobId", EmitDefaultValue = false)]
        public Guid JobID { get; set; }

        [DataMember(Name = "conclusion")]
        public TaskResult Conclusion { get; set; }

        [DataMember(Name = "outputs", EmitDefaultValue = false)]
        public Dictionary<string, VariableValue> Outputs { get; set; }

        [DataMember(Name = "stepResults", EmitDefaultValue = false)]
        public IList<StepResult> StepResults { get; set; }

        [DataMember(Name = "annotations", EmitDefaultValue = false)]
        public IList<Annotation> Annotations { get; set; }

        [DataMember(Name = "environmentUrl", EmitDefaultValue = false)]
        public string EnvironmentUrl { get; set; }
    }
}
