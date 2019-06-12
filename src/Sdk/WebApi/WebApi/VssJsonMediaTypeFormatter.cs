using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Patch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Constants related to JSON serialization customizations 
    /// </summary>
    public static class VssJsonSerializationConstants
    {
        /// <summary>
        /// Header which indicates to serialize enums as numbers.
        /// </summary>
        public const string EnumsAsNumbersHeader = "enumsAsNumbers";

        /// <summary>
        /// Header which indicates to serialize dates using the Microsoft Ajax date format
        /// </summary>
        public const string MsDateFormatHeader = "msDateFormat";

        /// <summary>
        /// Header which indicates to return a root array in a JSON response rather than wrapping it in an object
        /// </summary>
        public const string NoArrayWrapHeader = "noArrayWrap";
    }


    public class VssJsonMediaTypeFormatter : JsonMediaTypeFormatter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bypassSafeArrayWrapping">This should typically be false.  A true value will cause the wrapping to be skipped which is neccesary when creating ObjectContent from arrays on client to prepare a request</param>
        public VssJsonMediaTypeFormatter(bool bypassSafeArrayWrapping = false)
            : this(bypassSafeArrayWrapping, false, false)
        {
        }

        public VssJsonMediaTypeFormatter(bool bypassSafeArrayWrapping, bool enumsAsNumbers = false, bool useMsDateFormat = false)
        {
            this.SetSerializerSettings(bypassSafeArrayWrapping, enumsAsNumbers, useMsDateFormat);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bypassSafeArrayWrapping">This should typically be false.  A true value will cause the wrapping to be skipped which is neccesary when creating ObjectContent from arrays on client to prepare a request</param>
        public VssJsonMediaTypeFormatter(HttpRequestMessage request, bool bypassSafeArrayWrapping = false)
        {
            Request = request;
            SerializerSettings.Context = new StreamingContext(0, Request);

            bool enumsAsNumbers = String.Equals("true", GetAcceptHeaderOptionValue(request, VssJsonSerializationConstants.EnumsAsNumbersHeader), StringComparison.OrdinalIgnoreCase);
            bool useMsDateFormat = String.Equals("true", GetAcceptHeaderOptionValue(request, VssJsonSerializationConstants.MsDateFormatHeader), StringComparison.OrdinalIgnoreCase);
            if (!bypassSafeArrayWrapping)
            {
                // We can override the array-wrapping behavior based on a header. We haven't supported Firefox pre-2.0 in years, and even then the array prototype exploit
                // is not possible if you have to send a custom header in the request to get a JSON array to be returned.
                bypassSafeArrayWrapping = String.Equals("true", GetAcceptHeaderOptionValue(request, VssJsonSerializationConstants.NoArrayWrapHeader), StringComparison.OrdinalIgnoreCase);
            }

            this.SetSerializerSettings(bypassSafeArrayWrapping, enumsAsNumbers, useMsDateFormat);
        }

        private void SetSerializerSettings(bool bypassSafeArrayWrapping, bool enumsAsNumbers, bool useMsDateFormat)
        {
            this.SerializerSettings.ContractResolver = GetContractResolver(enumsAsNumbers);

            if (!enumsAsNumbers)
            {
                // Serialze enums as camelCased string values
                this.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
            }

            if (useMsDateFormat)
            {
                this.SerializerSettings.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            }

            m_bypassSafeArrayWrapping = bypassSafeArrayWrapping;

            EnumsAsNumbers = enumsAsNumbers;
            UseMsDateFormat = useMsDateFormat;
        }

        protected virtual IContractResolver GetContractResolver(bool enumsAsNumbers)
        {
            if (enumsAsNumbers)
            {
                return new VssCamelCasePropertyNamesPreserveEnumsContractResolver();
            }
            else
            {
                return new VssCamelCasePropertyNamesContractResolver();
            }
        }

        protected HttpRequestMessage Request { get; private set; }

        /// <summary>
        /// Whether or not to wrap a root array into an object with a "value" property equal to the array.
        /// This protects against an old browser vulnerability (Firefox 2.0) around overriding the 'Array'
        /// prototype and referencing a REST endpoint through in a script tag, and stealing the results
        /// cross-origin.
        /// </summary>
        public Boolean BypassSafeArrayWrapping
        {
            get
            {
                return m_bypassSafeArrayWrapping;
            }
            set
            {
                m_bypassSafeArrayWrapping = value;
            }
        }

        /// <summary>
        /// True if enums are serialized as numbers rather than user-friendly strings
        /// </summary>
        public Boolean EnumsAsNumbers { get; private set; }

        /// <summary>
        /// True if dates are to be emitted using MSJSON format rather than ISO format.
        /// </summary>
        public Boolean UseMsDateFormat { get; private set; }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            if (GetType().Equals(typeof(VssJsonMediaTypeFormatter))) // ensures we don't return a VssJsonMediaTypeFormatter when this instance is not a VssJsonMediaTypeFormatter
            {
                return new VssJsonMediaTypeFormatter(request, m_bypassSafeArrayWrapping);
            }
            else
            {
                return base.GetPerRequestFormatterInstance(type, request, mediaType); // basically returns this instance
            }
        }

        private String GetAcceptHeaderOptionValue(HttpRequestMessage request, String acceptOptionName)
        {
            foreach (var header in request.Headers.Accept)
            {
                foreach (var parameter in header.Parameters)
                {
                    if (String.Equals(parameter.Name, acceptOptionName, StringComparison.OrdinalIgnoreCase))
                    {
                        return parameter.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Because JSON PATCH and JSON both use the JSON format, we explicitly are 
        /// blocking the default JSON formatter from being able to read the PATCH 
        /// format.
        /// </summary>
        public override bool CanReadType(Type type)
        {
            return !type.IsOfType(typeof(IPatchDocument<>));
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            // Do not wrap byte arrays as this is incorrect behavior (they are written as base64 encoded strings and
            // not as array objects like other types).

            Type typeToWrite = type;
            if (!m_bypassSafeArrayWrapping
                && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo())
                && !type.Equals(typeof(Byte[]))
                && !type.Equals(typeof(JObject)))
            {
                typeToWrite = typeof(VssJsonCollectionWrapper);

                // IEnumerable will need to be materialized if they are currently not.
                object materializedValue = value is ICollection || value is string ?
                    value : // Use the regular input if it is already materialized or it is a string
                    ((IEnumerable)value)?.Cast<Object>().ToList() ?? value; // Otherwise, try materialize it

                value = new VssJsonCollectionWrapper((IEnumerable)materializedValue);
            }
            return base.WriteToStreamAsync(typeToWrite, value, writeStream, content, transportContext);
        }

        private bool m_bypassSafeArrayWrapping;
    }
}
