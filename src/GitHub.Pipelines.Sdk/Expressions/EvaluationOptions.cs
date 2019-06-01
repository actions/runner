using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class EvaluationOptions
    {
        public EvaluationOptions()
        {
        }

        public EvaluationOptions(EvaluationOptions copy)
        {
            if (copy != null)
            {
                Converters = copy.Converters;
                MaxMemory = copy.MaxMemory;
                TimeZone = copy.TimeZone;
                UseCollectionInterfaces = copy.UseCollectionInterfaces;
            }
        }

        /// <summary>
        /// Converters allow types to be coerced into data that is friendly
        /// for expression functions to operate on it.
        /// 
        /// As each node in the expression tree is evaluated, converters are applied.
        /// When a node's result matches a converter type, the result is intercepted
        /// by the converter, and converter result is used instead.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IDictionary<Type, Converter<Object, ConversionResult>> Converters { get; set; }

        public Int32 MaxMemory { get; set; }

        public TimeZoneInfo TimeZone { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean UseCollectionInterfaces { get; set; } // Feature flag for now behavior
    }
}
