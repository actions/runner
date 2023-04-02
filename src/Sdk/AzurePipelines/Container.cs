using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace Runner.Server.Azure.Devops {

public class MountReadonlyConfig {
    public bool? Work { get; set; }
    public bool? Externals { get; set; }
    public bool? Tools { get; set; }
    public bool? Tasks { get; set; }
}

public class Container {
    public string Alias { get; set; }
    public string Image { get; set; }
    public string Endpoint { get; set; }
    public Dictionary<string, string> Env { get; set; }
    public bool? MapDockerSocket { get; set; }
    public string Options { get; set; }
    public string[] Ports { get; set; }
    public string[] Volumes { get; set; }
    public MountReadonlyConfig MountReadonly { get; set; }

    public Container Parse(TemplateToken source) {
        // alias can also be a number in template parameters
        if(source is LiteralToken aliasName) {
            Alias = aliasName.ToString();
        } else {
            var jobToken = source.AssertMapping("job-root");
            foreach(var kv in jobToken) {
                switch(kv.Key.AssertString("key").Value) {
                    case "alias":
                        Alias = kv.Value.AssertLiteralString("alias");
                    break;
                    case "image":
                        Image = kv.Value.AssertLiteralString("image");
                    break;
                    case "endpoint":
                        Endpoint = kv.Value.AssertLiteralString("endpoint");
                    break;
                    case "env":
                        Env = new Dictionary<string, string>();
                        foreach(var ekv in kv.Value.AssertMapping("env mapping")) {
                            Env[ekv.Key.AssertString("env key").Value] = ekv.Value.AssertLiteralString("env value");
                        }
                    break;
                    case "mapDockerSocket":
                        MapDockerSocket = kv.Value.AssertAzurePipelinesBoolean("mapDockerSocket");
                    break;
                    case "options":
                        Options = kv.Value.AssertLiteralString("options");
                    break;
                    case "ports":
                        Ports = kv.Value.AssertSequence("ports").Select(p => p.AssertLiteralString("pm")).ToArray();
                    break;
                    case "volumes":
                        Volumes = kv.Value.AssertSequence("volumes").Select(p => p.AssertLiteralString("pm")).ToArray();
                    break;
                    case "mountReadOnly":
                        MountReadonly = new MountReadonlyConfig();
                        foreach(var ekv in kv.Value.AssertMapping("env mapping")) {
                            switch(ekv.Key.AssertString("env key").Value) {
                                case "work":
                                    MountReadonly.Work = ekv.Value.AssertAzurePipelinesBoolean("bool value");
                                break;
                                case "externals":
                                    MountReadonly.Externals = ekv.Value.AssertAzurePipelinesBoolean("bool value");
                                break;
                                case "tools":
                                    MountReadonly.Tools = ekv.Value.AssertAzurePipelinesBoolean("bool value");
                                break;
                                case "tasks":
                                    MountReadonly.Tasks = ekv.Value.AssertAzurePipelinesBoolean("bool value");
                                break;
                            }
                        }
                    break;
                }
            }
        }
        return this;
    }

    public PipelineContextData ToContextData(string name = null) {
        var container = new DictionaryContextData();
        if(Alias != null) {
            container["alias"] = new StringContextData(Alias);
            return container;
        }
        if(name != null) {
            container["container"] = new StringContextData(name);
        }
        container["image"] = new StringContextData(Image);
        if(MapDockerSocket != null) {
            container["mapDockerSocket"] = new StringContextData(MapDockerSocket.Value.ToString());
        }
        if(Options?.Length > 0) {
            container["options"] = new StringContextData(Options);
        }
        if(Endpoint?.Length > 0) {
            container["endpoint"] = new StringContextData(Endpoint);
        }
        if(Volumes?.Length > 0) {
            var volumes = new ArrayContextData();
            foreach(var vol in Volumes) {
                volumes.Add(new StringContextData(vol));
            }
            container["volumes"] = volumes;
        }
        if(Ports?.Length > 0) {
            var ports = new ArrayContextData();
            foreach(var port in Ports) {
                ports.Add(new StringContextData(port));
            }
            container["ports"] = ports;
        }
        if(Env?.Count > 0) {
            var env = new DictionaryContextData();
            foreach(var inp in Env) {
                env[inp.Key] = new StringContextData(inp.Value);
            }
            container["env"] = env;
        }
        if(MountReadonly != null) {
            var mountReadonly = new DictionaryContextData();
            if(MountReadonly.Work != null) {
                mountReadonly["work"] = new StringContextData(MountReadonly.Work.Value.ToString());
            }
            if(MountReadonly.Externals != null) {
                mountReadonly["externals"] = new StringContextData(MountReadonly.Externals.Value.ToString());
            }
            if(MountReadonly.Tools != null) {
                mountReadonly["tools"] = new StringContextData(MountReadonly.Tools.Value.ToString());
            }
            if(MountReadonly.Tasks != null) {
                mountReadonly["tasks"] = new StringContextData(MountReadonly.Tasks.Value.ToString());
            }
            container["mountReadonly"] = mountReadonly;
        }
        return container;
    }
}
}