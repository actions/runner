using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using GitHub.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// The class to represent a collection of REST reference links.
    /// </summary>
    [XmlRoot("ReferenceLinks")]
    [JsonConverter(typeof(ReferenceLinksConverter))]
    public class ReferenceLinks : ICloneable, IXmlSerializable
    {
        /// <summary>
        /// The internal representation of the reference links.
        /// </summary>
        private IDictionary<string, object> referenceLinks = new Dictionary<string, object>();

        /// <summary>
        /// Helper method to easily add a reference link to the dictionary.
        /// If the specified name has already been added, the subsequent calls
        /// to AddLink will create a list of reference links for the name.
        /// </summary>
        /// <param name="name">The name of the reference link.</param>
        /// <param name="href">The href the reference link refers to.</param>
        /// <param name="securedObject">The implementation for securedObject.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void AddLink(string name, string href, ISecuredObject securedObject)
        {
            if (referenceLinks.ContainsKey(name))
            {
                IList<ReferenceLink> links;
                if (referenceLinks[name] is ReferenceLink)
                {
                    // promote to a list of links
                    links = new List<ReferenceLink>();
                    links.Add((ReferenceLink)referenceLinks[name]);
                    referenceLinks[name] = links;
                }
                else
                {
                    links = (IList<ReferenceLink>)referenceLinks[name];
                }

                links.Add(new ReferenceLink(securedObject) { Href = href });
            }
            else
            {
                referenceLinks[name] = new ReferenceLink(securedObject) { Href = href };
            }
        }

        /// <summary>
        /// Helper method to easily add a reference link to the dictionary.
        /// If the specified name has already been added, the subsequent calls
        /// to AddLink will create a list of reference links for the name.
        /// </summary>
        /// <param name="name">The name of the reference link.</param>
        /// <param name="href">The href the reference link refers to.</param>
        public void AddLink(string name, string href)
        {
            AddLink(name, href, null);
        }

        /// <summary>
        /// Helper method to easily add a reference link to the dictionary if href is not null or empty value.
        /// If the specified name has already been added, the subsequent calls to AddLink will create a list of reference links for the name.
        /// </summary>
        /// <param name="name">The name of the reference link.</param>
        /// <param name="href">The href the reference link refers to.</param>
        public void AddLinkIfIsNotEmpty(string name, string href)
        {
            if (!string.IsNullOrEmpty(href))
            {
                AddLink(name, href, null);
            }
        }

        Object ICloneable.Clone()
        {
            return this.Clone();
        }

        /// <summary>
        /// Creates a deep copy of the ReferenceLinks.
        /// </summary>
        /// <returns>A deep copy of the ReferenceLinks</returns>
        public ReferenceLinks Clone()
        {
            ReferenceLinks linksCloned = new ReferenceLinks();
            this.CopyTo(linksCloned);
            return linksCloned;
        }

        /// <summary>
        /// Copies the ReferenceLinks to another ReferenceLinks.
        /// </summary>
        /// <param name="target"></param>
        public void CopyTo(ReferenceLinks target)
        {
            CopyTo(target, null);
        }

        /// <summary>
        /// Copies the ReferenceLinks to another ReferenceLinks and secures using the specified object.
        /// </summary>
        /// <param name="target"></param>
        public void CopyTo(ReferenceLinks target, ISecuredObject securedObject)
        {
            ArgumentUtility.CheckForNull(target, nameof(target));

            foreach (var link in this.Links)
            {
                if (link.Value is IList<ReferenceLink>)
                {
                    var hrefs = link.Value as IList<ReferenceLink>;
                    if (hrefs != null)
                    {
                        foreach (var href in hrefs)
                        {
                            target.AddLink(link.Key, href.Href, securedObject);
                        }
                    }
                }
                else if (link.Value is ReferenceLink)
                {
                    var href = link.Value as ReferenceLink;
                    if (href != null)
                    {
                        target.AddLink(link.Key, href.Href, securedObject);
                    }
                }
            }
        }
        
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(List<ReferenceLink>));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
            {
                return;
            }

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");

                reader.ReadStartElement("key");
                var key = (string)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                var value = (List<ReferenceLink>)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                if (value.Count == 1)
                {
                    referenceLinks.Add(key, value[0]);
                }
                else if (value.Count > 1)
                {
                    referenceLinks.Add(key, value);
                }

                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(List<ReferenceLink>));

            foreach (var item in this.referenceLinks)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, item.Key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                var links = item.Value as List<ReferenceLink>;
                if (links == null)
                {
                    links = new List<ReferenceLink>()
                    {
                        (ReferenceLink)item.Value
                    };
                }

                valueSerializer.Serialize(writer, links);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// The readonly view of the links.  Because Reference links are readonly,
        /// we only want to expose them as read only.
        /// </summary>
        public IReadOnlyDictionary<string, object> Links
        {
            get
            {
                return new ReadOnlyDictionary<string, object>(referenceLinks);
            }
        }

        /// <summary>
        /// The json converter to represent the reference links as a dictionary.
        /// </summary>
        private class ReferenceLinksConverter : VssSecureJsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(ReferenceLinks));
            }

            /// <summary>
            /// Because ReferenceLinks is a dictionary of either a single 
            /// ReferenceLink or an array of ReferenceLinks, we need custom
            /// deserialization to correctly rebuild the dictionary.
            /// </summary>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var unresolvedLinks = serializer.Deserialize<Dictionary<String, object>>(reader);
                if (unresolvedLinks == null)
                {
                    return null;
                }

                var links = new Dictionary<String, Object>();
                foreach (var entry in unresolvedLinks)
                {
                    if (String.IsNullOrEmpty(entry.Key))
                    {
                        throw new JsonSerializationException(WebApiResources.InvalidReferenceLinkFormat());
                    }

                    JToken token = entry.Value as JToken;
                    if (token != null)
                    {
                        switch (token.Type)
                        {
                            case JTokenType.Array:
                                using (var tokenReader = token.CreateReader())
                                {
                                    links[entry.Key] = serializer.Deserialize<IList<ReferenceLink>>(tokenReader);
                                }
                                break;

                            case JTokenType.Object:
                                using (var tokenReader = token.CreateReader())
                                {
                                    links[entry.Key] = serializer.Deserialize<ReferenceLink>(tokenReader);
                                }
                                break;

                            default:
                                throw new JsonSerializationException(WebApiResources.InvalidReferenceLinkFormat());
                        }
                    }
                    else if (entry.Value is ReferenceLink || entry.Value is IList<ReferenceLink>)
                    {
                        links[entry.Key] = entry.Value;
                    }
                    else
                    {
                        throw new JsonSerializationException(WebApiResources.InvalidReferenceLinkFormat());
                    }
                }

                return new ReferenceLinks { referenceLinks = links };
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                base.WriteJson(writer, value, serializer);
                serializer.Serialize(writer, ((ReferenceLinks)value).referenceLinks);
            }
        }
    }
}
