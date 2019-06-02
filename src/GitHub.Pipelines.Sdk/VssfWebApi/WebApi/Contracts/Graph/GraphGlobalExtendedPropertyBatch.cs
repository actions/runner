using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Graph
{
    [DataContract]
    public class GraphGlobalExtendedPropertyBatch
    {
        public GraphGlobalExtendedPropertyBatch(
            IEnumerable<SubjectDescriptor> subjectDescriptors,
            IEnumerable<string> propertyNameFilters)
        {
            SubjectDescriptors = subjectDescriptors;
            PropertyNameFilters = propertyNameFilters;
        }

        [DataMember]
        public IEnumerable<SubjectDescriptor> SubjectDescriptors { get; private set; }

        [DataMember]
        public IEnumerable<string> PropertyNameFilters { get; private set; }
    }
}
