using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.FormInput
{
    [DataContract]
    public enum InputFilterOperator
    {
        [EnumMember]
        Equals,

        [EnumMember]
        NotEquals
    }

    /// <summary>
    /// Defines a filter for subscription inputs. The filter matches a set of inputs
    /// if any (one or more) of the groups evaluates to true.
    /// </summary>
    [DataContract]
    public class InputFilter
    {
        /// <summary>
        /// Groups of input filter expressions. This filter matches a set of inputs
        /// if any (one or more) of the groups evaluates to true.
        /// </summary>
        [DataMember]
        public List<InputFilterCondition> Conditions { get; set; }
    }

    /// <summary>
    /// An expression which can be applied to filter a list of subscription inputs
    /// </summary>
    [DataContract]
    public class InputFilterCondition
    {
        /// <summary>
        /// The Id of the input to filter on
        /// </summary>
        [DataMember]
        public String InputId { get; set; }

        /// <summary>
        /// The operator applied between the expected and actual input value
        /// </summary>
        [DataMember]
        public InputFilterOperator Operator { get; set; }

        /// <summary>
        /// The "expected" input value to compare with the actual input value
        /// </summary>
        [DataMember]
        public String InputValue { get; set; }

        /// <summary>
        /// Whether or not to do a case sensitive match
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean CaseSensitive { get; set; }
    }
}