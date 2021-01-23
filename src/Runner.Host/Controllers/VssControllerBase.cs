using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runner.Host.Controllers
{
    public class VssControllerBase : ControllerBase {

        // private Boolean HasContent(Microsoft.AspNetCore.Http.HttpRequest request)
        // {
        //     if (request != null &&
        //         request?.Headers != null &&
        //         (!request.Headers.ContentLength.HasValue ||
        //             (request.Headers.ContentLength.HasValue && request.Headers.ContentLength != 0)))
        //     {
        //         return true;
        //     }

        //     return false;
        // }
        // private Boolean IsJsonRequest(
        //     Microsoft.AspNetCore.Http.HttpRequest request)
        // {
        //     if (HasContent(request)
        //         && request.Headers != null && request.ContentType != null
        //         && !String.IsNullOrEmpty(request.ContentType))
        //     {
        //         return (0 == String.Compare("application/json", request.ContentType, StringComparison.OrdinalIgnoreCase));
        //     }

        //     return false;
        // }

        protected async Task<T> FromBody<T>() {
            using(var reader = new StreamReader(Request.Body)) {
                string text = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(text);
            }
            // Boolean isJson = IsJsonRequest(Request);
            // bool mismatchContentType = false;
            // try
            // {
            //     //deal with wrapped collections in json
            //     if (isJson &&
            //         typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) &&
            //         !typeof(Byte[]).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()) &&
            //         !typeof(JObject).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
            //     {
            //         // expect it to come back wrapped, if it isn't it is a bug!
            //         var wrapper = await ReadJsonContentAsync<VssJsonCollectionWrapper<T>>(response, cancellationToken).ConfigureAwait(false);
            //         return wrapper.Value;
            //     }
            //     else if (isJson)
            //     {
            //         return await ReadJsonContentAsync<T>(response, cancellationToken).ConfigureAwait(false);
            //     }
            // }
            // catch (JsonReaderException)
            // {
            //     // We thought the content was JSON but failed to parse. 
            //     // In this case, do nothing and utilize the HandleUnknownContentType call below
            //     mismatchContentType = true;
            // }

            // if (HasContent(response))
            // {
            //     return await HandleInvalidContentType<T>(response, mismatchContentType).ConfigureAwait(false);
            // }
            // else
            // {
            //     return default(T);
            // }
        }

        protected async Task<KeyValuePair<T, JObject>> FromBody2<T>() {
            using(var reader = new StreamReader(Request.Body)) {
                string text = await reader.ReadToEndAsync();
                var obj = JObject.Parse(text);
                return new KeyValuePair<T, JObject>(JsonConvert.DeserializeObject<T>(text), obj);
            }
        }

        protected async Task<FileStreamResult> Ok<T>(T obj) {
            return new FileStreamResult(await new ObjectContent<T>(obj, new VssJsonMediaTypeFormatter()).ReadAsStreamAsync(), "application/json");
        }
    }
}