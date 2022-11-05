using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.WebApi;
using GitHub.DistributedTask.Expressions2;

namespace Runner.Server.Azure.Devops {
    public class Context {
        public ExpressionFlags Flags { get; set; }
        public IFileProvider FileProvider { get; set; }
        public GitHub.DistributedTask.ObjectTemplating.ITraceWriter TraceWriter { get; set; }
        public IVariablesProvider VariablesProvider { get; set; }
        public string RepositoryAndRef { get; set; }
        public string CWD { get; set; }
        public Dictionary<string, string> Repositories { get; set; }

        public ITaskByNameAndVersionProvider TaskByNameAndVersion { get; set; }

        public Context Clone() {
            return MemberwiseClone() as Context;
        }

        public Context ChildContext(MappingToken template, string path = null) {
            if(Repositories == null) {
                Repositories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            var childContext = Clone();
            foreach(var kv in template) {
                switch(kv.Key.AssertString("key").Value) {
                    case "resources":
                        foreach(var resource in kv.Value.AssertMapping("resources")) {
                            switch(resource.Key.AssertString("").Value) {
                                case "repositories":
                                    // Use a global dictionary, since resources needs to be resolved for the localcheckout step
                                    // childContext.Repositories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    foreach(var rawresource in resource.Value.AssertSequence("cres")) {
                                        string alias = null;
                                        string name = null;
                                        string @ref = "main";
                                        foreach(var rkv in rawresource.AssertMapping("")) {
                                            switch(rkv.Key.AssertString("").Value) {
                                                case "repository":
                                                    alias = rkv.Value.AssertString("").Value;
                                                break;
                                                case "name":
                                                    name = rkv.Value.AssertString("").Value;
                                                break;
                                                case "ref":
                                                    @ref = rkv.Value.AssertString("").Value;
                                                break;
                                            }
                                        }
                                        childContext.Repositories[alias] = $"{name}@{@ref}";
                                    }
                                break;
                            } 
                        }
                    break;
                }
            }
            if(path != null) {
                if(path.Contains("@")) {
                    var pathComp = path.Split("@", 2);
                    childContext.CWD = AzureDevops.RelativeTo(".", $"{pathComp[0]}/..");
                    childContext.RepositoryAndRef = string.Equals(pathComp[1], "self", StringComparison.OrdinalIgnoreCase) ? null : Repositories[pathComp[1]];
                } else {
                    childContext.CWD = AzureDevops.RelativeTo(CWD, $"{path}/..");
                }
            }
            return childContext;
        }
    }
}
