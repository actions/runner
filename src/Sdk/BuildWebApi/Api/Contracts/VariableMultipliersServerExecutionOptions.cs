using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents options for running a phase based on values specified by a list of variables.
    /// </summary>
    [DataContract]
    public class VariableMultipliersServerExecutionOptions : ServerTargetExecutionOptions, IVariableMultiplierExecutionOptions
    {
        public VariableMultipliersServerExecutionOptions()
            : this(null)
        {
        }

        internal VariableMultipliersServerExecutionOptions(
            ISecuredObject securedObject)
            : base(ServerTargetExecutionType.VariableMultipliers, securedObject)
        {
            MaxConcurrency = 1;
            ContinueOnError = false;
        }

        /// <summary>
        /// The maximum number of server jobs to run in parallel.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        [DefaultValue(1)]
        public Int32 MaxConcurrency {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether failure of one job should prevent the phase from running in other jobs.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean ContinueOnError
        {
            get;
            set;
        }

        /// <summary>
        /// The list of variables used to determine the matrix of phases to run.
        /// </summary>
        public List<String> Multipliers
        {
            get
            {
                if (m_multipliers == null)
                {
                    m_multipliers = new List<String>();
                }
                return m_multipliers;
            }
            set
            {
                m_multipliers = new List<String>(value);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedMultipliers, ref m_multipliers, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_multipliers, ref m_serializedMultipliers);
        }

        [DataMember(Name = "Multipliers", EmitDefaultValue = false)]
        private List<String> m_serializedMultipliers;

        private List<String> m_multipliers;
    }
}
