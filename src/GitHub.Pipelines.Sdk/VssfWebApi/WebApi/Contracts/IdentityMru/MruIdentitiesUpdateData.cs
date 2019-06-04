using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Identity.Mru
{
    [DataContract]
    public class JsonPatchOperationData<T>
    {
        public JsonPatchOperationData(string op, string path, T value)
        {
            Op = op;
            Path = path;
            Value = value;
        }

        [DataMember]
        public string Op { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public T Value { get; set; }
    }

    [DataContract]
    public class MruIdentitiesUpdateData : JsonPatchOperationData<IList<Guid>>
    {
        public MruIdentitiesUpdateData(IList<Guid> identityIds, string operationType)
            : base(operationType, "/identityIds", identityIds)
        { }
    }
}
