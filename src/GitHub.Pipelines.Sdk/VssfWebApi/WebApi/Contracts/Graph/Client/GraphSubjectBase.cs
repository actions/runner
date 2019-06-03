using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Xml;
using Newtonsoft.Json;

namespace GitHub.Services.Graph.Client
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [XmlSerializableDataContract]
    public abstract class GraphSubjectBase : IXmlSerializable
    {
        /// The descriptor is the primary way to reference the graph subject while the system is running. This field
        /// will uniquely identify the same graph subject across both Accounts and Organizations.
        /// </summary>
        public SubjectDescriptor Descriptor { get; protected set; }

        /// <summary>
        /// The descriptor is the primary way to reference the graph subject while the system is running. This field
        /// will uniquely identify the same graph subject across both Accounts and Organizations.
        /// </summary>
        [DataMember(Name = "Descriptor", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "Descriptor", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private string DescriptorString
        {
            get { return Descriptor.ToString(); }
            set { Descriptor = SubjectDescriptor.FromString(value); }
        }

        /// <summary>
        /// This is the non-unique display name of the graph subject. To change this field, you must alter its value in the
        /// source provider.
        /// </summary>
        [DataMember]
        [JsonProperty]
        public string DisplayName { get; protected set; }

        /// <summary>
        /// This field contains zero or more interesting links about the graph subject. These links may be invoked to obtain additional
        /// relationships or more detailed information about this graph subject.
        /// </summary>
        [DataMember(Name = "_links", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "_links", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [XmlIgnore] // ReferenceLinks type does not currently support XML serialization (#1164908 for tracking)
        public ReferenceLinks Links { get; protected set; }

        /// <summary>
        /// This url is the full route to the source resource of this graph subject.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Url { get; protected set; }

        // only for serialization
        protected GraphSubjectBase() { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected GraphSubjectBase(
            SubjectDescriptor descriptor,
            string displayName,
            ReferenceLinks links,
            string url)
        {
            Descriptor = descriptor;
            DisplayName = displayName;
            Links = links;
            Url = url;
        }

        XmlSchema IXmlSerializable.GetSchema() { return null; }

        void IXmlSerializable.ReadXml(XmlReader reader) => reader.ReadDataMemberXml(this);

        void IXmlSerializable.WriteXml(XmlWriter writer) => writer.WriteDataMemberXml(this);
    }
}
