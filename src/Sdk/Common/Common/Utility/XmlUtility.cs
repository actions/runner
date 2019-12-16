using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace GitHub.Services.Common.Internal
{

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class XmlUtility
    {
        internal static FileStream OpenFile(String path, FileShare sharing, Boolean saveFile)
        {
            XmlDocument noXmlDocument;
            return OpenFileHelper(path, sharing, saveFile, false, out noXmlDocument);
        }

        internal static XmlDocument OpenXmlFile(out FileStream file, String path, FileShare sharing, Boolean saveFile)
        {
            XmlDocument xmlDocument;
            file = OpenFileHelper(path, sharing, saveFile, true, out xmlDocument);

            return xmlDocument;
        }

        private static FileStream OpenFileHelper(String path, FileShare sharing, Boolean saveFile, Boolean loadAsXmlDocument, out XmlDocument xmlDocument)
        {
            const int RetryCount = 10;
            FileStream file = null;
            xmlDocument = null;

            if (String.IsNullOrEmpty(path))
            {
                return null;
            }

            // If the file doesn't exist or an exception is thrown while trying to check that, we don't have
            // a cache file.
            if (!saveFile && !File.Exists(path))
            {
                return null;
            }

            int retries = 0;
            Random random = null;
            while (retries <= RetryCount)
            {
                try
                {
                    // Make sure the user hasn't made the file read-only if we are writing the file.
                    FileAccess fileAccess = FileAccess.Read;
                    FileMode fileMode = FileMode.Open;
                    if (saveFile)
                    {
                        fileAccess = FileAccess.ReadWrite;
                        fileMode = FileMode.OpenOrCreate;
                    }

                    file = new FileStream(path, fileMode, fileAccess, sharing);
                    
                    if (loadAsXmlDocument)
                    {
                        XmlReaderSettings settings = new XmlReaderSettings()
                        {
                            DtdProcessing = DtdProcessing.Prohibit,
                            XmlResolver = null,
                        };

                        using (XmlReader xmlReader = XmlReader.Create(file, settings))
                        {
                            xmlDocument = new XmlDocument();
                            xmlDocument.Load(xmlReader);
                        }
                    }

                    return file;
                }
                catch (Exception exception)
                {
                    if (file != null)
                    {
                        file.Dispose();
                        file = null;
                    }

                    if (exception is OperationCanceledException)
                    {
                        // Do not swallow the CancelException.
                        throw;
                    }
                    else if (exception is IOException || exception is UnauthorizedAccessException || exception is XmlException)
                    {
                        // If there was no cache file on disk, optionally create one.
                        if (saveFile)
                        {
                            try
                            {
                                // Create the directory if it does not exist.
                                if (exception is DirectoryNotFoundException)
                                {
                                    String dir = Path.GetDirectoryName(path);
                                    Directory.CreateDirectory(dir);
                                }

                                // Reset attributes (file might be read-only)
                                if (exception is UnauthorizedAccessException)
                                {
                                    File.SetAttributes(path, FileAttributes.Normal);
                                }

                                xmlDocument = null;
                                return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                            }
                            catch (Exception newException) when (newException is IOException || newException is UnauthorizedAccessException)
                            {
                                if (retries >= RetryCount)
                                {
                                    throw new AggregateException(exception, newException);
                                }
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else if (retries >= RetryCount)
                    {
                        throw new VssServiceException(CommonResources.ErrorReadingFile(Path.GetFileName(path), exception.Message), exception);
                    }
                }

                // Wait before trying again.
                if (random == null)
                {
                    random = new Random();
                }

                int sleepTime = random.Next(1, 150);
                Task.Delay(sleepTime).Wait();
                retries++;
            }

            // Should never get to here
            Debug.Fail("Code should be unreachable.");
            return null;
        }

        internal static void AddXmlAttribute(XmlNode node, String attrName, String value)
        {
            if (value != null)
            {
                XmlAttribute attr = node.OwnerDocument.CreateAttribute(null, attrName, null);
                node.Attributes.Append(attr);
                attr.InnerText = value;
            }
        }

        /// <summary>
        /// Returns a single shared instance of secured XML reader settings.
        /// </summary>
        /// <remarks>
        /// The main configuration that is set is to "Harden or Disable XML Entity Resolution",
        /// which disallows resolving entities during XML parsing.
        ///
        /// DO NOT USE this method if you need to resolved XML DTD entities.
        /// </remarks>
        public static XmlReaderSettings SecureReaderSettings
        {
            get
            {
                if (s_safeSettings == null)
                {
                    XmlReaderSettings settings = new XmlReaderSettings()
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                    };

                    s_safeSettings = settings;
                }

                return s_safeSettings;
            }
        }

        public static XmlDocument GetDocument(Stream input)
        {
            XmlDocument doc = new XmlDocument();

            using (XmlReader xmlReader = XmlReader.Create(input, SecureReaderSettings))
            {
                doc.Load(xmlReader);
            }

            return doc;
        }

        public static XmlDocument GetDocument(string xml)
        {
            XmlDocument doc = new XmlDocument();
            using (StringReader stringReader = new StringReader(xml))
            using (XmlReader xmlReader = XmlReader.Create(stringReader, SecureReaderSettings))
            {
                doc.Load(xmlReader);
            }

            return doc;
        }

        public static XmlDocument GetDocumentFromPath(string path)
        {
            XmlDocument doc = new XmlDocument();

            using (XmlReader xmlReader = XmlReader.Create(path, SecureReaderSettings))
            {
                doc.Load(xmlReader);
            }

            return doc;
        }

        public static DateTime ToDateTime(String s)
        {
            DateTime time = XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);

            // As of Dev11, WIT uses TeamFoundationClientProxy when it used to use TeamFoundationSoapProxy.
            // In Orcas, the WIT server would return DateTime strings which were UTC over-the-wire, but were not specified as such.
            // e.g. "01/28/2011T22:00:00.000" instead of "01/28/2011T22:00:00.000Z"
            // We need to handle that case now. If the time is unspecified, we'll assume it to be Utc.
            if (time.Kind == DateTimeKind.Unspecified &&
                time != DateTime.MinValue &&
                time != DateTime.MaxValue)
            {
                time = DateTime.SpecifyKind(time, DateTimeKind.Utc);
            }

            // Convert all year one values to DateTime.MinValue, a flag value meaning the date is not set.
            // We don't want the timezone set on DateTime.MinValue...
            if (time.Year == 1)
            {
                time = DateTime.MinValue;
            }
            else
            {
                time = time.ToLocalTime();
            }

            return time;
        }

        public static DateTime ToDateOnly(String s)
        {
            // we intentionally don't want to call ToLocalTime for converting Date only
            // because we ignore both Time and TimeZone.
            return XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);
        }

        public static String ToStringDateOnly(DateTime d)
        {
            return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static String ToString(DateTime d)
        {
            Debug.Assert(d == DateTime.MinValue || d == DateTime.MaxValue || d.Kind != DateTimeKind.Unspecified, "DateTime kind is unspecified instead of Local or Utc.");
            return XmlConvert.ToString(d, XmlDateTimeSerializationMode.RoundtripKind);
        }

        public static void ObjectToXmlElement(XmlWriter writer, String element, Object o)
        {
            if (o == null)
            {
                writer.WriteStartElement(element);
                writer.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                writer.WriteEndElement();
            }
            else
            {
                String clrTypeName = o.GetType().FullName;
                String soapType = null, soapValue = null, soapNamespaceUri = null;
                switch (clrTypeName)
                {
                    case "System.Boolean":
                        soapType = "boolean";
                        soapValue = XmlConvert.ToString((Boolean)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Byte":
                        soapType = "unsignedByte";
                        soapValue = XmlConvert.ToString((Byte)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Byte[]":
                        soapType = "base64Binary";
                        byte[] array = (byte[])o;
                        soapValue = Convert.ToBase64String(array, 0, array.Length);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Char":
                        soapType = "char";
                        soapValue = XmlConvert.ToString((UInt16)((Char)o));
                        soapNamespaceUri = "http://microsoft.com/wsdl/types/";
                        break;
                    case "System.DateTime":
                        soapType = "dateTime";
                        soapValue = ToString((DateTime)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Decimal":
                        soapType = "decimal";
                        soapValue = XmlConvert.ToString((Decimal)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Double":
                        soapType = "double";
                        soapValue = XmlConvert.ToString((Double)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Guid":
                        soapType = "guid";
                        soapValue = XmlConvert.ToString((Guid)o);
                        soapNamespaceUri = "http://microsoft.com/wsdl/types/";
                        break;
                    case "System.Int16":
                        soapType = "short";
                        soapValue = XmlConvert.ToString((Int16)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Int32":
                        soapType = "int";
                        soapValue = XmlConvert.ToString((Int32)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Int64":
                        soapType = "long";
                        soapValue = XmlConvert.ToString((Int64)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.Single":
                        soapType = "float";
                        soapValue = XmlConvert.ToString((Single)o);
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    case "System.String":
                        soapType = "string";
                        soapValue = (String)o;
                        soapNamespaceUri = "http://www.w3.org/2001/XMLSchema";
                        break;
                    default:
                        if (o.GetType().IsArray)
                        {
                            Debug.Assert(o.GetType().GetArrayRank() == 1, "ERROR: Cannot serialize multi-dimensional arrays");

                            writer.WriteStartElement(element);
                            writer.WriteAttributeString("type", "http://www.w3.org/2001/XMLSchema-instance", "ArrayOfAnyType");
                            ArrayOfObjectToXml<Object>(writer, (Object[])o, null, "anyType", true, false, ObjectToXmlElement);
                            writer.WriteEndElement();
                            return;
                        }
                        else
                        {
                            Debug.Fail("Unknown object type for serialization " + clrTypeName);
                            throw new ArgumentException(CommonResources.UnknownTypeForSerialization(clrTypeName));
                        }
                }

                writer.WriteStartElement(element);
                writer.WriteStartAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteQualifiedName(soapType, soapNamespaceUri);
                writer.WriteEndAttribute();
                writer.WriteValue(soapValue);
                writer.WriteEndElement();
            }
        }

        public static Object ObjectFromXmlElement(XmlReader reader)
        {
            String soapTypeName = reader.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
            if (!String.IsNullOrEmpty(soapTypeName))
            {
                String[] components = soapTypeName.Split(new char[] { ':' }, StringSplitOptions.None);
                if (components.Length == 2)
                {
                    soapTypeName = components[1];
#if DEBUG
                    String ns = reader.LookupNamespace(components[0]);
                    if (!String.IsNullOrEmpty(ns) &&
                        !ns.Equals("http://www.w3.org/2001/XMLSchema", StringComparison.OrdinalIgnoreCase) &&
                        !ns.Equals("http://microsoft.com/wsdl/types/", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Fail("Unknown namespace encountered for object type " + ns);
                        reader.ReadOuterXml();
                        return null;
                    }
#endif
                }

                switch (soapTypeName)
                {
                    case "base64Binary":
                        String str = StringFromXmlElement(reader);
                        if (str != null)
                        {
                            return Convert.FromBase64String(str);
                        }
                        return ZeroLengthArrayOfByte;
                    case "boolean":
                        return XmlConvert.ToBoolean(StringFromXmlElement(reader));
                    case "char":
                        return (Char)XmlConvert.ToInt16(StringFromXmlElement(reader));  // Char goes over the wire as short
                    case "dateTime":
                        return ToDateTime(StringFromXmlElement(reader));
                    case "decimal":
                        return XmlConvert.ToDecimal(StringFromXmlElement(reader));
                    case "double":
                        return XmlConvert.ToDouble(StringFromXmlElement(reader));
                    case "float":
                        return XmlConvert.ToSingle(StringFromXmlElement(reader));
                    case "int":
                        return XmlConvert.ToInt32(StringFromXmlElement(reader));
                    case "guid":
                        return XmlConvert.ToGuid(StringFromXmlElement(reader));
                    case "long":
                        return XmlConvert.ToInt64(StringFromXmlElement(reader));
                    case "short":
                        return XmlConvert.ToInt16(StringFromXmlElement(reader));
                    case "string":
                        return StringFromXmlElement(reader);
                    case "unsignedByte":
                        return XmlConvert.ToByte(StringFromXmlElement(reader));
                    case "ArrayOfAnyType":
                        return ArrayOfObjectFromXml(reader);
                    default:
                        Debug.Fail("Unknown object type encountered " + soapTypeName);
                        throw new ArgumentException(CommonResources.UnknownTypeForSerialization(soapTypeName));
                }
            }
            else if (reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true")
            {
                reader.ReadInnerXml();
                return null;
            }

            return null;
        }

        public static void ToXml(XmlWriter writer, String element, Object[] array)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }

            if (!String.IsNullOrEmpty(element))
            {
                writer.WriteStartElement(element);
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    throw new ArgumentNullException("array[" + i + "]");
                }
                ObjectToXmlElement(writer, "anyType", array[i]);
            }

            if (!String.IsNullOrEmpty(element))
            {
                writer.WriteEndElement();
            }
        }

        public static Object[] ArrayOfObjectFromXml(XmlReader reader)
        {
            List<Object> list = new List<Object>();
            bool empty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes &&
                        reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true")
                    {
                        list.Add(null);
                        reader.Read();
                    }
                    else
                    {
                        list.Add(ObjectFromXmlElement(reader));
                    }
                }
                reader.ReadEndElement();
            }
            return list.ToArray();
        }

        public static void ToXmlElement(XmlWriter writer, String elementName, XmlNode node)
        {
            if (node == null)
            {
                return;
            }

            writer.WriteStartElement(elementName);
            node.WriteTo(writer);
            writer.WriteEndElement();
        }

        public static XmlNode XmlNodeFromXmlElement(XmlReader reader)
        {
            // Advance the reader so we are at the contents of the node rather than
            // starting at the root node. Typically the root node will be the name of
            // a property, parameter, or result, and we should not include this in the
            // resulting XML.
            reader.Read();

            XmlDocument document = new XmlDocument
            {
                PreserveWhitespace = false
            };
            document.Load(reader);

            // Call Normalize to ensure that we don't create a whole bunch of additional XmlText elements
            // for blobs of, for example, HTML content.
            document.Normalize();

            reader.ReadEndElement();
            return document.DocumentElement;
        }

        public static DateTime DateFromXmlAttribute(XmlReader reader)
        {
            return ToDateOnly(StringFromXmlAttribute(reader));
        }

        public static DateTime DateFromXmlElement(XmlReader reader)
        {
            return ToDateOnly(StringFromXmlElement(reader));
        }

        public static void DateToXmlAttribute(XmlWriter writer, String name, DateTime value)
        {
            StringToXmlAttribute(writer, name, ToStringDateOnly(value));
        }

        public static void DateToXmlElement(XmlWriter writer, String name, DateTime value)
        {
            StringToXmlElement(writer, name, ToStringDateOnly(value));
        }

        public static Boolean BooleanFromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToBoolean(StringFromXmlAttribute(reader));
        }

        public static DateTime DateTimeFromXmlAttribute(XmlReader reader)
        {
            return ToDateTime(StringFromXmlAttribute(reader));
        }

        public static DateTime DateTimeFromXmlElement(XmlReader reader)
        {
            return ToDateTime(StringFromXmlElement(reader));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, DateTime value)
        {
            StringToXmlAttribute(writer, name, ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, DateTime value)
        {
            StringToXmlElement(writer, name, ToString(value));
        }

        public static void ToXml(XmlWriter writer, String element, byte[] array)
        {
            // Omit zero-length arrays to save bandwidth.
            if (array == null || array.Length == 0)
            {
                return;
            }
            writer.WriteElementString(element, Convert.ToBase64String(array, 0, array.Length));
        }

        public static void ToXmlAttribute(XmlWriter writer, String attr, byte[] array)
        {
            // Omit zero-length arrays to save bandwidth.
            if (array == null || array.Length == 0)
            {
                return;
            }
            writer.WriteAttributeString(attr, Convert.ToBase64String(array, 0, array.Length));
        }

        private static XmlReaderSettings s_safeSettings;

        public static String ToString(Uri uri)
        {
            return uri.AbsoluteUri;
        }

        public static Uri ToUri(String s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return null;
            }
            else
            {
                return new Uri(s);
            }
        }

        public static T EnumFromXmlText<T>(XmlReader reader)
        {
            String s = StringFromXmlText(reader);
            s = s.Replace(' ', ',');
            return (T)Enum.Parse(typeof(T), s, true);
        }

        public static void EnumToXmlText<T>(XmlWriter writer, String ignored, T value)
        {
            String s = Enum.Format(typeof(T), value, "G");
            s = s.Replace(",", "");
            writer.WriteString(s);
        }

        public static void EnumToXmlAttribute<T>(XmlWriter writer, String attr, T value)
        {
            String s = Enum.Format(typeof(T), value, "G");
            s = s.Replace(",", "");
            writer.WriteAttributeString(attr, s);
        }

        public static T EnumFromXmlAttribute<T>(XmlReader reader)
        {
            String s = StringFromXmlAttribute(reader);
            s = s.Replace(' ', ',');
            return (T)Enum.Parse(typeof(T), s, true);
        }

        public static void EnumToXmlElement<T>(XmlWriter writer, String element, T value)
        {
            String s = Enum.Format(typeof(T), value, "G");
            s = s.Replace(",", "");
            writer.WriteElementString(element, s);
        }

        public static T EnumFromXmlElement<T>(XmlReader reader)
        {
            String s = StringFromXmlElement(reader);
            s = s.Replace(' ', ',');
            return (T)Enum.Parse(typeof(T), s, true);
        }

        public static T[] ArrayOfObjectFromXml<T>(
            XmlReader reader,
            String arrayElementName,
            Boolean inline,
            Func<XmlReader, T> objectFromXmlElement)
        {
            return ArrayOfObjectFromXml<T>(null, reader, arrayElementName, inline, (x, y) => objectFromXmlElement(y));
        }

        public static T[] ArrayOfObjectFromXml<T>(
            IServiceProvider serviceProvider,
            XmlReader reader,
            String arrayElementName,
            Boolean inline,
            Func<IServiceProvider, XmlReader, T> objectFromXmlElement)
        {
            List<T> list = new List<T>();
            bool empty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            if (!inline)
            {
                reader.Read();
            }

            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element && (!inline || reader.Name == arrayElementName))
                {
                    if (reader.HasAttributes && reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true")
                    {
                        list.Add(default(T));
                        reader.Read();
                    }
                    else
                    {
                        list.Add(objectFromXmlElement(serviceProvider, reader));
                    }
                }
                reader.ReadEndElement();
            }
            return list.ToArray();
        }

        /// <summary>
        /// Writes an array of objects to xml using the provided callback function to serialize individual objects
        /// within the array.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the array</typeparam>
        /// <param name="writer">The xml writer for serialization</param>
        /// <param name="array">The array to be serialized</param>
        /// <param name="arrayName">The name of the array root element</param>
        /// <param name="arrayElementName">The name of the array elements</param>
        /// <param name="inline">True if the array elements should be written inline, or false to create the root node</param>
        /// <param name="allowEmptyArrays">True if an empty array should be serialized, false to omit empty arrays</param>
        /// <param name="objectToXmlElement">A callback function for serializing an individual array element</param>
        public static void ArrayOfObjectToXml<T>(
            XmlWriter writer,
            T[] array,
            String arrayName,
            String arrayElementName,
            Boolean inline,
            Boolean allowEmptyArrays,
            Action<XmlWriter, String, T> objectToXmlElement)
        {
            if (array == null)
            {
                return;
            }

            if (array.Length == 0)
            {
                if (allowEmptyArrays && !String.IsNullOrEmpty(arrayName))
                {
                    writer.WriteStartElement(arrayName);
                    writer.WriteEndElement();
                }
                return;
            }

            if (!inline)
            {
                writer.WriteStartElement(arrayName);

                for (Int32 i = 0; i < array.Length; i = i + 1)
                {
                    if (array[i] == null)
                    {
                        writer.WriteStartElement(arrayElementName);
                        writer.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                        writer.WriteEndElement();
                    }
                    else
                    {
                        objectToXmlElement(writer, arrayElementName, array[i]);
                    }
                }
                writer.WriteEndElement();
            }
            else
            {
                for (Int32 i = 0; i < array.Length; i = i + 1)
                {
                    if (array[i] == null)
                    {
                        writer.WriteStartElement(arrayElementName);
                        writer.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                        writer.WriteEndElement();
                    }
                    else
                    {
                        objectToXmlElement(writer, arrayElementName, array[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Writes an <c>System.Collections.Generic.IEnumerable&lt;T&gt;</c> of objects to xml using the provided
        /// callback function to serialize individual objects.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the array</typeparam>
        /// <param name="writer">The xml writer for serialization</param>
        /// <param name="array">The array to be serialized</param>
        /// <param name="arrayName">The name of the array root element</param>
        /// <param name="arrayElementName">The name of the array elements</param>
        /// <param name="inline">True if the array elements should be written inline, or false to create the root node</param>
        /// <param name="allowEmptyArrays">True if an empty array should be serialized, false to omit empty arrays</param>
        /// <param name="objectToXmlElement">A callback function for serializing an individual array element</param>
        public static void EnumerableOfObjectToXml<T>(
            XmlWriter writer,
            IEnumerable<T> enumerable,
            String arrayName,
            String arrayElementName,
            Boolean inline,
            Boolean allowEmptyArrays,
            Action<XmlWriter, String, T> objectToXmlElement)
        {
            // Optionally omit zero-length enumerables to save bandwidth
            if (enumerable == null)
            {
                return;
            }

            if (!enumerable.Any())
            {
                if (allowEmptyArrays && !String.IsNullOrEmpty(arrayName))
                {
                    writer.WriteStartElement(arrayName);
                    writer.WriteEndElement();
                }
                return;
            }

            if (!inline)
            {
                writer.WriteStartElement(arrayName);

                foreach (T item in enumerable)
                {
                    if (item == null)
                    {
                        writer.WriteStartElement(arrayElementName);
                        writer.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                        writer.WriteEndElement();
                    }
                    else
                    {
                        objectToXmlElement(writer, arrayElementName, item);
                    }
                }
                writer.WriteEndElement();
            }
            else
            {
                foreach (T item in enumerable)
                {
                    if (item == null)
                    {
                        writer.WriteStartElement(arrayElementName);
                        writer.WriteAttributeString("nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                        writer.WriteEndElement();
                    }
                    else
                    {
                        objectToXmlElement(writer, arrayElementName, item);
                    }
                }
            }
        }

        public static Boolean BooleanFromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToBoolean(StringFromXmlElement(reader));
        }

        public static Byte ByteFromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToByte(StringFromXmlAttribute(reader));
        }

        public static Byte ByteFromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToByte(StringFromXmlElement(reader));
        }

        public static Char CharFromXmlAttribute(XmlReader reader)
        {
            return (Char)XmlConvert.ToInt32(StringFromXmlAttribute(reader));
        }

        public static Char CharFromXmlElement(XmlReader reader)
        {
            return (Char)XmlConvert.ToInt32(StringFromXmlElement(reader));
        }

        public static Double DoubleFromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToDouble(StringFromXmlAttribute(reader));
        }

        public static Double DoubleFromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToDouble(StringFromXmlElement(reader));
        }

        public static Guid GuidFromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToGuid(StringFromXmlAttribute(reader));
        }

        public static Guid GuidFromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToGuid(StringFromXmlElement(reader));
        }

        public static Int16 Int16FromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToInt16(StringFromXmlAttribute(reader));
        }

        public static Int16 Int16FromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToInt16(StringFromXmlElement(reader));
        }

        public static Int32 Int32FromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToInt32(StringFromXmlAttribute(reader));
        }

        public static Int32 Int32FromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToInt32(StringFromXmlElement(reader));
        }

        public static Int64 Int64FromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToInt64(StringFromXmlAttribute(reader));
        }

        public static Int64 Int64FromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToInt64(StringFromXmlElement(reader));
        }

        public static Single SingleFromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToSingle(StringFromXmlAttribute(reader));
        }

        public static Single SingleFromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToSingle(StringFromXmlElement(reader));
        }

        public static String StringFromXmlAttribute(XmlReader reader)
        {
            return GetCachedString(reader.Value);
        }

        public static String StringFromXmlElement(XmlReader reader)
        {
            String str = String.Empty;
            Boolean isEmpty = reader.IsEmptyElement;
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            reader.Read();
            if (!isEmpty)
            {
                // We don't expect the server to send back a CDATA section, but the client OM
                // may use the FromXml methods to read a hand-edited xml file.
                if (reader.NodeType == XmlNodeType.CDATA ||
                    reader.NodeType == XmlNodeType.Text ||
                    reader.NodeType == XmlNodeType.Whitespace)
                {
                    str = GetCachedString(reader.ReadContentAsString().Replace("\n", "\r\n"));
                    reader.ReadEndElement();
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    // in the case where the element is empty/whitespace such as <value></value>, we need to read past the end element
                    reader.ReadEndElement();
                }
            }

            return str;
        }

        public static String StringFromXmlText(XmlReader reader)
        {
            String str = String.Empty;
            if (reader.NodeType == XmlNodeType.CDATA ||
                reader.NodeType == XmlNodeType.Text ||
                reader.NodeType == XmlNodeType.Whitespace)
            {
                str = GetCachedString(reader.ReadContentAsString().Replace("\n", "\r\n"));
            }
            return str;
        }

        public static TimeSpan TimeSpanFromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToTimeSpan(StringFromXmlAttribute(reader));
        }

        public static TimeSpan TimeSpanFromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToTimeSpan(StringFromXmlElement(reader));
        }

        public static UInt16 UInt16FromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToUInt16(StringFromXmlAttribute(reader));
        }

        public static UInt16 UInt16FromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToUInt16(StringFromXmlElement(reader));
        }

        public static UInt32 UInt32FromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToUInt32(StringFromXmlAttribute(reader));
        }

        public static UInt32 UInt32FromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToUInt32(StringFromXmlElement(reader));
        }

        public static UInt64 UInt64FromXmlAttribute(XmlReader reader)
        {
            return XmlConvert.ToUInt64(StringFromXmlAttribute(reader));
        }

        public static UInt64 UInt64FromXmlElement(XmlReader reader)
        {
            return XmlConvert.ToUInt64(StringFromXmlElement(reader));
        }

        public static Uri UriFromXmlAttribute(XmlReader reader)
        {
            return ToUri(StringFromXmlAttribute(reader));
        }

        public static Uri UriFromXmlElement(XmlReader reader)
        {
            return ToUri(StringFromXmlElement(reader));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Boolean value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Byte value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Char value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString((Int32)value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Double value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Guid value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Int16 value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Int32 value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Int64 value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Single value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, String value)
        {
            StringToXmlAttribute(writer, name, value);
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, TimeSpan value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, UInt16 value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, UInt32 value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, UInt64 value)
        {
            StringToXmlAttribute(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlAttribute(XmlWriter writer, String name, Uri value)
        {
            StringToXmlAttribute(writer, name, ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, Boolean value)
        {
            StringToXmlElement(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, Byte value)
        {
            StringToXmlElement(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, Char value)
        {
            StringToXmlElement(writer, name, XmlConvert.ToString((Int32)value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, Double value)
        {
            StringToXmlElement(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, Guid value)
        {
            StringToXmlElement(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String element, Int16 value)
        {
            StringToXmlElement(writer, element, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String element, Int32 value)
        {
            StringToXmlElement(writer, element, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String element, Int64 value)
        {
            StringToXmlElement(writer, element, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, Single value)
        {
            StringToXmlElement(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, String value)
        {
            StringToXmlElement(writer, name, value);
        }

        public static void ToXmlElement(XmlWriter writer, String name, TimeSpan value)
        {
            StringToXmlElement(writer, name, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String element, UInt16 value)
        {
            StringToXmlElement(writer, element, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String element, UInt32 value)
        {
            StringToXmlElement(writer, element, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String element, UInt64 value)
        {
            StringToXmlElement(writer, element, XmlConvert.ToString(value));
        }

        public static void ToXmlElement(XmlWriter writer, String name, Uri value)
        {
            StringToXmlElement(writer, name, ToString(value));
        }

        public static void StringToXmlAttribute(XmlWriter writer, String name, String value)
        {
            writer.WriteAttributeString(name, value);
        }

        public static void StringToXmlElement(XmlWriter writer, String name, String value)
        {
            try
            {
                writer.WriteElementString(name, value);
            }
            catch (ArgumentException e)
            {
                Debug.Assert(e.Message.IndexOf("invalid character", StringComparison.OrdinalIgnoreCase) > 0, "Unexpected exception: " + e.ToString());
                throw new VssServiceException(CommonResources.StringContainsIllegalChars(), e);
            }
        }

        public static void StringToXmlText(XmlWriter writer, String str)
        {
            if (str == null)
            {
                return;
            }

            try
            {
                writer.WriteString(str);
            }
            catch (ArgumentException e)
            {
                Debug.Assert(e.Message.IndexOf("invalid character", StringComparison.OrdinalIgnoreCase) > 0, "Unexpected exception: " + e.ToString());
                throw new VssServiceException(CommonResources.StringContainsIllegalChars(), e);
            }
        }

        public static byte[] ArrayOfByteFromXml(XmlReader reader)
        {
            String str = StringFromXmlElement(reader);
            if (str != null)
            {
                return Convert.FromBase64String(str);
            }
            return ZeroLengthArrayOfByte;
        }

        public static byte[] ArrayOfByteFromXmlAttribute(XmlReader reader)
        {
            if (reader.Value.Length != 0)
            {
                return Convert.FromBase64String(reader.Value);
            }
            return ZeroLengthArrayOfByte;
        }

        public static byte[] ZeroLengthArrayOfByte
        {
            get
            {
                if (s_zeroLengthArrayOfByte == null)
                {
                    s_zeroLengthArrayOfByte = new byte[0];
                }
                return s_zeroLengthArrayOfByte;
            }
        }

        public static bool CompareXmlDocuments(string xml1, string xml2)
        {
            if (xml1 == xml2)
            {
                return true;
            }
            else if (string.IsNullOrEmpty(xml1) || string.IsNullOrEmpty(xml2))
            {
                return false;
            }

            XDocument x1 = XDocument.Parse(xml1);
            XDocument x2 = XDocument.Parse(xml2);

            return Compare(x1?.Root, x2?.Root);
        }

        private static bool Compare(XContainer x1, XContainer x2)
        {
            if (object.ReferenceEquals(x1, x2))
            {
                return true;
            }

            XElement e1 = x1 as XElement;
            XElement e2 = x2 as XElement;

            if (e1 != null && e2 != null)
            {
                if (!VssStringComparer.XmlNodeName.Equals(e1.Name.ToString(), e2.Name.ToString()) ||
                    !e1.Attributes().OrderBy(a => a.Name.ToString()).SequenceEqual(e2.Attributes().OrderBy(a => a.Name.ToString()), s_xmlAttributeComparer) ||
                    !VssStringComparer.XmlElement.Equals(e1.Value, e2.Value))
                {
                    return false;
                }

                return x1.Elements().OrderBy(xe => xe.Name.ToString()).SequenceEqual(x2.Elements().OrderBy(xe => xe.Name.ToString()), s_xmlElementComparer);
            }

            return false;
        }

        #region GetCachedString

        /// <summary>
        /// Strings are often duplicated in the XML returned by the server. To
        /// reduce the number of identical String instances, we keep a small
        /// cache of the last N strings to be deserialized off the wire.
        ///
        /// Send your deserialized strings through this method. If they match a
        /// recently deserialized string, the cached value will be returned and
        /// your deserialized string will be left in Gen0 for easy collection.
        /// </summary>
        private static String GetCachedString(String fromXml)
        {
            if (null == fromXml)
            {
                return null;
            }

            int fromXmlLength = fromXml.Length;

            // Don't cache large strings. They take a lot longer to compare.
            if (fromXmlLength > 256)
            {
                return fromXml;
            }

            if (fromXmlLength == 0)
            {
                return String.Empty;
            }

            String[] stringList = ts_stringList;

            // Set up the thread-static data structures if they have not yet
            // been initialized.
            if (null == stringList)
            {
                stringList = new String[c_stringCacheSize];
                ts_stringList = stringList;
            }

            // Check for a cache hit.
            for (int i = 0; i < c_stringCacheSize; i++)
            {
                String cachedString = stringList[i];

                if (null == cachedString)
                {
                    break;
                }

                // If the lengths or first characters are different, this
                // is not a hit.
                if (cachedString.Length != fromXmlLength ||
                    fromXml[0] != cachedString[0])
                {
                    continue;
                }

                // If the strings are 6 characters or longer, check the character
                // 5 characters from the end. Remember at this point we know the
                // strings are identical in length.
                if (fromXmlLength > 5 &&
                    fromXml[fromXmlLength - 5] != cachedString[fromXmlLength - 5])
                {
                    continue;
                }

                // OK, looks like a potential hit, let's verify with String.Equals.
                if (String.Equals(fromXml, cachedString, StringComparison.Ordinal))
                {
                    // This is a cache hit. Move it to the 0 position and shove
                    // everything else down.
                    for (int j = i - 1; j >= 0; j--)
                    {
                        stringList[j + 1] = stringList[j];
                    }

                    stringList[0] = cachedString;

                    return cachedString;
                }
            }

            // This is a cache miss. Evict the nth position, move everything else
            // down, and insert this at the 0 position.
            for (int i = c_stringCacheSize - 2; i >= 0; i--)
            {
                stringList[i + 1] = stringList[i];
            }

            stringList[0] = fromXml;

            return fromXml;
        }

        [ThreadStatic]
        private static String[] ts_stringList;

        // Size of the cache. Larger values mean more memory savings
        // but more time spent in GetCachedString.
        private const int c_stringCacheSize = 16;

        #endregion GetCachedString

        private class AttributeComparer : IEqualityComparer<XAttribute>
        {
            public bool Equals(XAttribute x, XAttribute y)
            {
                if (x == y)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return VssStringComparer.XmlAttributeName.Equals(x.Name.ToString(), y.Name.ToString()) &&
                    VssStringComparer.XmlAttributeValue.Equals(x.Value, y.Value);
            }

            public int GetHashCode(XAttribute obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                return obj.GetHashCode();
            }
        }

        private class ElementComparer : IEqualityComparer<XElement>
        {
            public bool Equals(XElement x, XElement y)
            {
                if (x == y)
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }
                return XmlUtility.Compare(x, y);
            }

            public int GetHashCode(XElement obj)
            {
                if (obj == null)
                {
                    return 0;
                }
                return obj.GetHashCode();
            }
        }

        private static byte[] s_zeroLengthArrayOfByte;
        private static readonly AttributeComparer s_xmlAttributeComparer = new AttributeComparer();
        private static readonly ElementComparer s_xmlElementComparer = new ElementComparer();
    }

    /// <summary>
    /// XML element writer class that automatically makes the closing WriteEndElement call
    /// during dispose.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class XmlElementWriterUtility : IDisposable
    {
        private XmlWriter m_xmlWriter;

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlElementWriterUtility(string elementName, XmlWriter xmlWriter)
        {
            m_xmlWriter = xmlWriter;
            m_xmlWriter.WriteStartElement(elementName);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            m_xmlWriter.WriteEndElement();
        }
    }
}
