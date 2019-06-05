using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Batching of subjects to lookup using the Graph API
    /// </summary>
    [DataContract]
    public class GraphSubjectLookup
    {
        [DataMember]
        public IEnumerable<GraphSubjectLookupKey> LookupKeys { get; private set; }

        public GraphSubjectLookup(IEnumerable<GraphSubjectLookupKey> lookupKeys)
        {
            LookupKeys = lookupKeys;
        }
    }

    [DataContract]
    public class GraphSubjectLookupKey
    {
        [DataMember]
        public SubjectDescriptor Descriptor { get; private set; }

        public GraphSubjectLookupKey(SubjectDescriptor descriptor)
        {
            Descriptor = descriptor;
        }
    }

    public static class GraphSubjectLookupExtensions
    {
        public static IEnumerable<SubjectDescriptor> ToSubjectDescriptors(this GraphSubjectLookup subjectLookup)
        {
            return subjectLookup.LookupKeys?.Select(x => x.Descriptor).ToList();
        }
    }
}
