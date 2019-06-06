using System;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{

    [DataContract]
    public abstract class VssJsonCollectionWrapperBase : ISecuredObject
    {
        protected VssJsonCollectionWrapperBase()
        {
        }

        public VssJsonCollectionWrapperBase(IEnumerable source)
        {
            if (source == null)
            {
                this.Count = 0;
            }
            else if (source is ICollection)
            {
                this.Count = ((ICollection)source).Count;
            }
            else 
            {
                this.Count = source.Cast<Object>().Count();
            }
            this._value = source;
        }

        [DataMember(Order=0)]
        public Int32 Count { get; private set; }

        //not serialized from here, see sub class...
        private IEnumerable _value;

        protected IEnumerable BaseValue
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => throw new NotImplementedException();

        int ISecuredObject.RequiredPermissions => throw new NotImplementedException();

        string ISecuredObject.GetToken() => throw new NotImplementedException();
        #endregion  
    }

    [DataContract]
    public sealed class VssJsonCollectionWrapper : VssJsonCollectionWrapperBase
    {
        public VssJsonCollectionWrapper()
            : base()
        {
        }
        public VssJsonCollectionWrapper(IEnumerable source)
            : base(source)
        {
        }

        [DataMember(Order = 1)]
        public IEnumerable Value
        {
            get
            {
                return BaseValue;
            }
            private set
            {
                BaseValue = value;
            }
        }

    }

    /// <summary>
    /// This class is used to serialized collections as a single
    ///  JSON object on the wire, to avoid serializing JSON arrays
    ///  directly to the client, which can be a security hole
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public sealed class VssJsonCollectionWrapper<T> : VssJsonCollectionWrapperBase
    {
        public VssJsonCollectionWrapper()
            : base()
        {
        }

        public VssJsonCollectionWrapper(IEnumerable source)
            :base (source)
        {
        }

        [DataMember]
        public T Value
        {
            get
            {
                return (T)BaseValue;
            }
            private set
            {
                BaseValue = (IEnumerable)value;
            }
        }
    }
}
