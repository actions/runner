// ************************************************************************************************
// Microsoft Team Foundation
//
// Microsoft Confidential
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// Contents:    Type definitions for artifact linking.
// ************************************************************************************************
using Microsoft.VisualStudio.Services.Common.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Common
{
    public class LinkFilter
    {
        public LinkFilter()
        {}

        public FilterType FilterType
        {
            get {return m_FilterType;}
            set {m_FilterType = value;}
        }

        public string[] FilterValues
        {
            get {return m_FilterValues;}
            set {m_FilterValues = value;}
        }

        internal static LinkFilter FromXml(XmlReader reader)
        {
            LinkFilter obj = new LinkFilter();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }
                }
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "FilterType":
                            obj.m_FilterType = XmlUtility.EnumFromXmlElement<FilterType>(reader);
                            break;
                        case "FilterValues":
                            obj.m_FilterValues = StringArrayFromXml(reader);
                            break;
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }
            return obj;
        }

        internal void ToXml(XmlWriter writer, String element)
        {
            writer.WriteStartElement(element);
            if (this.m_FilterType != FilterType.ToolType)
            {
                XmlUtility.EnumToXmlElement(writer, "FilterType", this.m_FilterType);
            }
            StringArrayToXmlElement(writer, "FilterValues", m_FilterValues);
            writer.WriteEndElement();
        }

        private static String[] StringArrayFromXml(XmlReader reader)
        {
            List<String> list = new List<string>();

            bool empty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes &&
                        reader.GetAttribute("xsi:nil") == "true")
                    {
                        Debug.Fail("Do we really have an API that returns string arrays with embedded nulls?");
                        list.Add(null);
                        reader.Read();
                    }
                    else
                    {
                        list.Add(XmlUtility.StringFromXmlElement(reader));
                    }
                }
                reader.ReadEndElement();
            }
            return list.ToArray();
        }

        private static void StringArrayToXmlElement(XmlWriter writer, String element, String[] array)
        {
            // Omit zero-length arrays to save bandwidth
            if (array == null || array.Length == 0)
            {
                return;
            }
            writer.WriteStartElement(element);
            for (int i = 0; i < array.Length; i++)
            {
                XmlUtility.ToXmlElement(writer, "string", array[i]);
            }
            writer.WriteEndElement();
        }

        public static LinkFilter[] LinkFilterArrayFromXml(XmlReader reader)
        {
            List<LinkFilter> list = new List<LinkFilter>();
            Boolean empty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes && reader.GetAttribute("xsi:nil") == "true")
                    {
                        list.Add(null);
                        reader.Read();
                    }
                    else
                    {
                        list.Add(FromXml(reader));
                    }
                }
                reader.ReadEndElement();
            }
            return list.ToArray();
        }

        public static void LinkFilterArrayToXml(XmlWriter writer, String element, LinkFilter[] array)
        {
            // Omit zero-length arrays to save bandwidth
            if (array == null || array.Length == 0)
            {
                return;
            }

            writer.WriteStartElement(element);

            for (Int32 i = 0; i < array.Length; i = i + 1)
            {
                if (array[i] == null)
                {
                    throw new ArgumentNullException("array[" + i + "]");
                }
                array[i].ToXml(writer, "LinkFilter");
            }
            writer.WriteEndElement();
        }

        FilterType m_FilterType;
        string[] m_FilterValues = null;
    }

    public enum FilterType
    {
        ToolType,
        ArtifactType,
        LinkType
    }

    public class ArtifactId
    {
        public ArtifactId()
        {}

        public ArtifactId(string tool, string artifactType, string specificId)
        {
            m_Tool = tool;
            m_ArtifactType = artifactType;
            m_ToolSpecificId = specificId;
        }

        public string VisualStudioServerNamespace
        {
            get {return m_VisualStudioServerNamespace;}
            set {m_VisualStudioServerNamespace = value;}
        }
        public string Tool
        {
            get {return m_Tool;}
            set {m_Tool = value;}
        } 
        public string ArtifactType
        {
            get {return m_ArtifactType;}
            set {m_ArtifactType = value;}
        }   
        public string ToolSpecificId
        {
            get {return m_ToolSpecificId;}
            set {m_ToolSpecificId = value;}
        }        
        private string m_VisualStudioServerNamespace = null;
        private string m_Tool = null;
        private string m_ArtifactType = null;
        private string m_ToolSpecificId = null;
        
	    public new string ToString()
        {
            return "< Namespace: " + m_VisualStudioServerNamespace + " | " +
                   "Tool: " + m_Tool + " | " +
                   "Artifact Type: " + m_ArtifactType + " | " +
                   "Tool Specific ID: " + m_ToolSpecificId + " >";
        }
    }

    public class Artifact
    {
        public string Uri
        {
            get {return m_Uri;}
            set {m_Uri = value;}
        } 

        public string ArtifactTitle
        {
            get {return m_ArtifactTitle;}
            set {m_ArtifactTitle = value;}
        }

        public string ExternalId
        {
            get { return m_ExternalId; }
            set { m_ExternalId = value; }
        }
        public ExtendedAttribute[] ExtendedAttributes
        {
            get {return m_ExtendedAttributes;}
            set {m_ExtendedAttributes = value;}
        } 
        public OutboundLink[] OutboundLinks
        {
            get {return m_OutboundLinks;}
            set {m_OutboundLinks = value;}
        }

        internal static Artifact FromXml(XmlReader reader)
        {
            Artifact obj = new Artifact();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }
                }
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "ArtifactTitle":
                            obj.m_ArtifactTitle = XmlUtility.StringFromXmlElement(reader);
                            break;
                        case "ExtendedAttributes":
                            obj.m_ExtendedAttributes = ExtendedAttribute.ExtendedAttributeArrayFromXml(reader);
                            break;
                        case "ExternalId":
                            obj.m_ExternalId = XmlUtility.StringFromXmlElement(reader);
                            break;
                        case "OutboundLinks":
                            obj.m_OutboundLinks = OutboundLink.OutboundLinkArrayFromXml(reader);
                            break;
                        case "Uri":
                            obj.m_Uri = XmlUtility.StringFromXmlElement(reader);
                            break;
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }
            return obj;
        }

        internal void ToXml(XmlWriter writer, String element)
        {
            writer.WriteStartElement(element);
            if (this.m_ArtifactTitle != null)
            {
                XmlUtility.ToXmlElement(writer, "ArtifactTitle", this.m_ArtifactTitle);
            }
            ExtendedAttribute.ExtendedAttributeArrayToXml(writer, "ExtendedAttributes", this.m_ExtendedAttributes);
            if (this.m_ExternalId != null)
            {
                XmlUtility.ToXmlElement(writer, "ExternalId", this.m_ExternalId);
            }
            OutboundLink.OutboundLinkArrayToXml(writer, "OutboundLinks", this.m_OutboundLinks);
            if (this.m_Uri != null)
            {
                XmlUtility.ToXmlElement(writer, "Uri", this.m_Uri);
            }
            writer.WriteEndElement();
        }

        public static Artifact[] ArtifactArrayFromXml(XmlReader reader)
        {
            List<Artifact> list = new List<Artifact>();
            Boolean empty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes && reader.GetAttribute("xsi:nil") == "true")
                    {
                        list.Add(null);
                        reader.Read();
                    }
                    else
                    {
                        list.Add(FromXml(reader));
                    }
                }
                reader.ReadEndElement();
            }
            return list.ToArray();
        }

        internal static void ArtifactArrayToXml(XmlWriter writer, String element, Artifact[] array)
        {
            // Omit zero-length arrays to save bandwidth
            if (array == null || array.Length == 0)
            {
                return;
            }

            writer.WriteStartElement(element);

            for (Int32 i = 0; i < array.Length; i = i + 1)
            {
                if (array[i] == null)
                {
                    throw new ArgumentNullException("array[" + i + "]");
                }
                array[i].ToXml(writer, "Artifact");
            }
            writer.WriteEndElement();
        }

        private string m_Uri = null;
        private string m_ArtifactTitle = null;
        private string m_ExternalId = null;
        private ExtendedAttribute[] m_ExtendedAttributes = null;
        private OutboundLink[] m_OutboundLinks = null;
    }

    public class ExtendedAttribute 
    {
        public string Name
        {
            get {return m_Name;}
            set {m_Name = value;}
        } 
        public string Value
        {
            get {return m_Value;}
            set {m_Value = value;}
        } 
        public string FormatString
        {
            get {return m_FormatString;}
            set {m_FormatString = value;}
        }

        internal static ExtendedAttribute FromXml(XmlReader reader)
        {
            ExtendedAttribute obj = new ExtendedAttribute();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }
                }
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "FormatString":
                            obj.m_FormatString = XmlUtility.StringFromXmlElement(reader);
                            break;
                        case "Name":
                            obj.m_Name = XmlUtility.StringFromXmlElement(reader);
                            break;
                        case "Value":
                            obj.m_Value = XmlUtility.StringFromXmlElement(reader);
                            break;
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }
            return obj;
        }

        internal void ToXml(XmlWriter writer, String element)
        {
            writer.WriteStartElement(element);
            if (this.m_FormatString != null)
            {
                XmlUtility.ToXmlElement(writer, "FormatString", this.m_FormatString);
            }
            if (this.m_Name != null)
            {
                XmlUtility.ToXmlElement(writer, "Name", this.m_Name);
            }
            if (this.m_Value != null)
            {
                XmlUtility.ToXmlElement(writer, "Value", this.m_Value);
            }
            writer.WriteEndElement();
        }

        internal static ExtendedAttribute[] ExtendedAttributeArrayFromXml(XmlReader reader)
        {
            List<ExtendedAttribute> list = new List<ExtendedAttribute>();
            Boolean empty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes && reader.GetAttribute("xsi:nil") == "true")
                    {
                        list.Add(null);
                        reader.Read();
                    }
                    else
                    {
                        list.Add(FromXml(reader));
                    }
                }
                reader.ReadEndElement();
            }
            return list.ToArray();
        }

        internal static void ExtendedAttributeArrayToXml(XmlWriter writer, String element, ExtendedAttribute[] array)
        {
            // Omit zero-length arrays to save bandwidth
            if (array == null || array.Length == 0)
            {
                return;
            }

            writer.WriteStartElement(element);

            for (Int32 i = 0; i < array.Length; i = i + 1)
            {
                if (array[i] == null)
                {
                    throw new ArgumentNullException("array[" + i + "]");
                }
                array[i].ToXml(writer, "ExtendedAttribute");
            }
            writer.WriteEndElement();
        }

        private string m_Name = null;
        private string m_Value = null;
        private string m_FormatString = null;
    }

    public class ArtifactLink
    {
        public string ReferringUri
        {
            get {return m_ReferringUri;}
            set {m_ReferringUri = value;}
        } 
        public string LinkType
        {
            get {return m_LinkType;}
            set {m_LinkType = value;}
        } 
        public string ReferencedUri
        {
            get {return m_ReferencedUri;}
            set {m_ReferencedUri = value;}
        } 
        private string m_ReferringUri = null;
        private string m_LinkType = null;
        private string m_ReferencedUri = null;
    }

    public class OutboundLink
    {
        public string LinkType
        {
            get {return m_LinkType;}
            set {m_LinkType = value;}
        } 
        public string ReferencedUri
        {
            get {return m_ReferencedUri;}
            set {m_ReferencedUri = value;}
        }

        internal static OutboundLink FromXml(XmlReader reader)
        {
            OutboundLink obj = new OutboundLink();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }
                }
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "LinkType":
                            obj.m_LinkType = XmlUtility.StringFromXmlElement(reader);
                            break;
                        case "ReferencedUri":
                            obj.m_ReferencedUri = XmlUtility.StringFromXmlElement(reader);
                            break;
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }
            return obj;
        }

        internal void ToXml(XmlWriter writer, String element)
        {
            writer.WriteStartElement(element);
            if (this.m_LinkType != null)
            {
                XmlUtility.ToXmlElement(writer, "LinkType", this.m_LinkType);
            }
            if (this.m_ReferencedUri != null)
            {
                XmlUtility.ToXmlElement(writer, "ReferencedUri", this.m_ReferencedUri);
            }
            writer.WriteEndElement();
        }

        internal static OutboundLink[] OutboundLinkArrayFromXml(XmlReader reader)
        {
            List<OutboundLink> list = new List<OutboundLink>();
            Boolean empty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes && reader.GetAttribute("xsi:nil") == "true")
                    {
                        list.Add(null);
                        reader.Read();
                    }
                    else
                    {
                        list.Add(FromXml(reader));
                    }
                }
                reader.ReadEndElement();
            }
            return list.ToArray();
        }

        internal static void OutboundLinkArrayToXml(XmlWriter writer, String element, OutboundLink[] array)
        {
            // Omit zero-length arrays to save bandwidth
            if (array == null || array.Length == 0)
            {
                return;
            }

            writer.WriteStartElement(element);

            for (Int32 i = 0; i < array.Length; i = i + 1)
            {
                if (array[i] == null)
                {
                    throw new ArgumentNullException("array[" + i + "]");
                }
                array[i].ToXml(writer, "OutboundLink");
            }
            writer.WriteEndElement();
        }

        private string m_LinkType = null;
        private string m_ReferencedUri = null;
    }
}
