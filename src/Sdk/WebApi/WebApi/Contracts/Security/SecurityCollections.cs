using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Security
{
    [CollectionDataContract(Name = "AccessControlEntries", ItemName = "AccessControlEntry")]
    public sealed class AccessControlEntriesCollection : List<AccessControlEntry>
    {
        public AccessControlEntriesCollection()
        {
        }

        public AccessControlEntriesCollection(IEnumerable<AccessControlEntry> source)
            : base(source)
        {
        }
    }

    /// <summary>
    /// A list of AccessControlList. An AccessControlList is meant to associate a set of AccessControlEntries with 
    /// a security token and its inheritance settings.
    /// </summary>
    [CollectionDataContract(Name = "AccessControlLists", ItemName = "AccessControlList")]
    public sealed class AccessControlListsCollection : List<AccessControlList>
    {
        public AccessControlListsCollection()
        {
        }

        public AccessControlListsCollection(IEnumerable<AccessControlList> source)
            : base(source)
        {
        }
    }
}
