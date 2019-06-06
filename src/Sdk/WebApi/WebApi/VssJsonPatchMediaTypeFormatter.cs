using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Patch;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// The media type formatter for json-patch.  It deserializes the incoming json
    /// into a JsonPatchDocument, and then calls PatchDocument.CreateFromJson
    /// which creates the strongly typed PatchDocument expected by the controller.
    /// 
    /// This is done to ensure all semantic validation can occur before the controller
    /// gets the object model. 
    /// </summary>
    public class VssJsonPatchMediaTypeFormatter : VssJsonMediaTypeFormatter
    {
        private MediaTypeHeaderValue JsonPatch = new MediaTypeHeaderValue("application/json-patch+json");

        public VssJsonPatchMediaTypeFormatter() : base()
        {
            // Clearing out all the media types since we only
            // want the json patch media type supported.
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(JsonPatch);
        }

        public override bool CanReadType(Type type)
        {
            return type.IsOfType(typeof(IPatchDocument<>));
        }

        public override bool CanWriteType(Type type)
        {
            return false;
        }

        public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            var result = await base.ReadFromStreamAsync(typeof(JsonPatchDocument), readStream, content, formatterLogger).ConfigureAwait(false);

            var createMethod = type.GetTypeInfo().DeclaredMethods.First(m => m.Name.Equals("CreateFromJson") && m.Attributes.HasFlag(MethodAttributes.Public | MethodAttributes.Static));

            try
            {
                var document = createMethod.Invoke(null, new object[] { result });
                return document;
            }
            catch (Exception ex)
            {
                // We don't want to show the TargetInvocationException, but 
                // rather the inner exception which contains the real failure.
                if (ex is TargetInvocationException &&
                    ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
