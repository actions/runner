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

namespace Runner.Server.Controllers
{
    public class VssControllerBase : ControllerBase {
        protected async Task<T> FromBody<T>() {
            using(var reader = new StreamReader(Request.Body)) {
                string text = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(text);
            }
        }

        protected async Task<KeyValuePair<T, JObject>> FromBody2<T>() {
            using(var reader = new StreamReader(Request.Body)) {
                string text = await reader.ReadToEndAsync();
                var obj = JObject.Parse(text);
                return new KeyValuePair<T, JObject>(JsonConvert.DeserializeObject<T>(text), obj);
            }
        }

        protected async Task<FileStreamResult> Ok<T>(T obj, bool bypassSafeArrayWrapping  = false) {
            return new FileStreamResult(await new ObjectContent<T>(obj, new VssJsonMediaTypeFormatter(bypassSafeArrayWrapping)).ReadAsStreamAsync(), "application/json");
        }
    }
}