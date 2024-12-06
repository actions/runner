using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Net.Mime;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.Runner.Worker;

namespace Runner.Client
{
    public class TypeHelper {
        public static string GetTypeName(System.Type t) {
            if(t == typeof(bool)){
                return "bool";
            } else if(t == typeof(string)) {
                return "string";
            } else if(t == typeof(string[])) {
                return "stringArray";
            } else if(t == typeof(int)) {
                return "int";
            }
            throw new InvalidOperationException(t.Name);
        }
    }
}