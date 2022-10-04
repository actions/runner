using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace Runner.Server.Azure.Devops {

public class MountReadonlyConfig {
    public bool Work { get; set; }
    public bool Externals { get; set; }
    public bool Tools { get; set; }
    public bool Tasks { get; set; }
}

public class Container {
    public string Image { get; set; }
    public string Endpoint { get; set; }
    public Dictionary<string, string> Env { get; set; }
    public bool MapDockerSocket { get; set; }
    public string Options { get; set; }
    public string[] Ports { get; set; }
    public string[] Volumes { get; set; }
    public MountReadonlyConfig MountReadonly { get; set; }
    public bool StringSource { get; set; }

    public Container Parse(TemplateToken source) {
        if(source is StringToken imageName) {
            Image = imageName.Value;
            StringSource = true;
        } else {
            var jobToken = source.AssertMapping("job-root");
            foreach(var kv in jobToken) {
                switch(kv.Key.AssertString("key").Value) {
                    case "image":
                        Image = kv.Value.AssertString("image").Value;
                    break;
                    case "endpoint":
                        Endpoint = kv.Value.AssertString("endpoint").Value;
                    break;
                    case "env":
                        Env = new Dictionary<string, string>();
                        foreach(var ekv in kv.Value.AssertMapping("env mapping")) {
                            Env[ekv.Key.AssertString("env key").Value] = ekv.Value.AssertString("env value").Value;
                        }
                    break;
                    case "mapDockerSocket":
                        MapDockerSocket = kv.Value.AssertBoolean("mapDockerSocket").Value;
                    break;
                    case "options":
                        Options = kv.Value.AssertString("options").Value;
                    break;
                    case "ports":
                        Ports = kv.Value.AssertSequence("ports").Select(p => p.AssertString("pm").Value).ToArray();
                    break;
                    case "volumes":
                        Volumes = kv.Value.AssertSequence("volumes").Select(p => p.AssertString("pm").Value).ToArray();
                    break;
                    case "mountReadOnly":
                        MountReadonly = new MountReadonlyConfig();
                        foreach(var ekv in kv.Value.AssertMapping("env mapping")) {
                            switch(ekv.Key.AssertString("env key").Value) {
                                case "work":
                                    MountReadonly.Work = ekv.Value.AssertBoolean("bool value").Value;
                                break;
                                case "externals":
                                    MountReadonly.Externals = ekv.Value.AssertBoolean("bool value").Value;
                                break;
                                case "tools":
                                    MountReadonly.Tools = ekv.Value.AssertBoolean("bool value").Value;
                                break;
                                case "tasks":
                                    MountReadonly.Tasks = ekv.Value.AssertBoolean("bool value").Value;
                                break;
                            }
                        }
                    break;
                }
            }
        }
        return this;
    }

    public PipelineContextData ToContextData() {
        if(StringSource) {
            return new StringContextData(Image);
        }
        var container = new DictionaryContextData();
        container["image"] = new StringContextData(Image);
        if(MapDockerSocket) {
            container["mapDockerSocket"] = new BooleanContextData(MapDockerSocket);
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
            mountReadonly["work"] = new BooleanContextData(MountReadonly.Work);
            mountReadonly["externals"] = new BooleanContextData(MountReadonly.Externals);
            mountReadonly["tools"] = new BooleanContextData(MountReadonly.Tools);
            mountReadonly["tasks"] = new BooleanContextData(MountReadonly.Tasks);
            container["mountReadonly"] = mountReadonly;
        }
        return container;
    }
}
}