using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runner.Server.Controllers
{
    public class VssControllerBase : ControllerBase {
        private string _serverUrl = null;
        protected string ServerUrl { get {
            if(string.IsNullOrEmpty(_serverUrl)) {
                var serverurl = new UriBuilder();
                serverurl.Scheme = Request.Scheme;
                if(string.IsNullOrEmpty(Request.Host.Host)) {
                    serverurl.Host = HttpContext.Connection.LocalIpAddress.ToString();
                    serverurl.Port = HttpContext.Connection.LocalPort;
                } else {
                    serverurl.Host = Request.Host.Host;
                    serverurl.Port = Request.Host.Port ?? -1;
                }
                return serverurl.Uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port, UriFormat.UriEscaped);
            } else {
                return _serverUrl;
            }
        } set => _serverUrl = value; }
        
        protected IConfiguration Configuration { get; }
        protected VssControllerBase(IConfiguration configuration) {
            Configuration = configuration;
            ServerUrl = configuration?.GetSection("Runner.Server")?.GetValue<string>("ServerUrl");
        }

        protected async Task<KeyValuePair<T, JObject>> FromBody2<T>(HashAlgorithm hash = null, string expected = null) {
            var stream = Request.Body;
            if(hash != null && expected != null) {
                stream = new MemoryStream();
                await Request.Body.CopyToAsync(stream);
                stream.Position = 0;
                var calculatedhash = await hash.ComputeHashAsync(stream);
                StringBuilder stringBuilder = new StringBuilder();
                foreach(byte b in calculatedhash) {
                    stringBuilder.AppendFormat("{0:x2}", b);
                }
                if(stringBuilder.ToString() != expected) {
                    throw new UnauthorizedAccessException();
                }
                stream.Position = 0;
            }
            using(var reader = new StreamReader(stream)) {
                string text = await reader.ReadToEndAsync();
                var obj = JObject.Parse(text);
                return new KeyValuePair<T, JObject>(JsonConvert.DeserializeObject<T>(text, new VssJsonMediaTypeFormatter().SerializerSettings), obj);
            }
        }

        protected async Task<FileStreamResult> Ok<T>(T obj, bool bypassSafeArrayWrapping  = false) {
            return new FileStreamResult(await new ObjectContent<T>(obj, new VssJsonMediaTypeFormatter(bypassSafeArrayWrapping)).ReadAsStreamAsync(), "application/json");
        }
    }
}