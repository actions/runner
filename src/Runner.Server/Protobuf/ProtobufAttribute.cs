using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Runner.Server {
    public class ProtobufBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (!bindingContext.HttpContext.Request.HasJsonContentType())
            {
                throw new BadHttpRequestException(
                    "Request content type was not a recognized JSON content type.",
                    StatusCodes.Status415UnsupportedMediaType);
            }

            using var sr = new StreamReader(bindingContext.HttpContext.Request.Body);
            var str = await sr.ReadToEndAsync();

            var valueType = bindingContext.ModelType;
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            var descriptor = (MessageDescriptor)bindingContext.ModelType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
            var obj = parser.Parse(str, descriptor);

            bindingContext.Result = ModelBindingResult.Success(obj);
        }
    }

    public class ProtobufAttribute : ModelBinderAttribute {
        public ProtobufAttribute() : base(typeof(ProtobufBinder)) {

        }
    }
}