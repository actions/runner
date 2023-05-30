using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TimelineRecordComparer : IEqualityComparer<TimelineRecord>
    {
        public int Compare(TimelineRecord tr1, TimelineRecord tr2)
        {
            if (tr1.Id != tr2.Id)
            {
                return tr1.Id.CompareTo(tr2.Id);
            }
            else if (tr1.ParentId != tr2.ParentId)
            {
                return tr1.ParentId.GetValueOrDefault().CompareTo(tr2.ParentId.GetValueOrDefault());
            }
            else if (tr1.Identifier != tr2.Identifier)
            {
                return tr1.Identifier.CompareTo(tr2.Identifier);
            }
            else if (tr1.RecordType != tr2.RecordType)
            {
                return tr1.RecordType.CompareTo(tr2.RecordType);
            }
            else if (tr1.Name != tr2.Name)
            {
                return tr1.Name.CompareTo(tr2.Name);
            }
            else return 0;
        }

        public bool Equals(TimelineRecord x, TimelineRecord y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode([DisallowNull] TimelineRecord obj)
        {
            return obj.GetHashCode();
        }
    }
}