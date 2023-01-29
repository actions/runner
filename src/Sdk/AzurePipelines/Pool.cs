using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;

namespace Runner.Server.Azure.Devops {
public class Pool {
    public string Name { get; set; }
    public string VmImage { get; set; }
    public string[] Demands { get; set; }

    public Pool Parse(Runner.Server.Azure.Devops.Context context, TemplateToken source) {
        var poolToken = source.AssertMapping("pool-root");
        foreach(var kv in poolToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "name":
                    Name = kv.Value is NullToken ? null : kv.Value.AssertString("name").Value;
                break;
                case "vmImage":
                    VmImage = kv.Value.AssertString("name").Value;
                break;
                case "demands":
                    Demands = (from dep in kv.Value.AssertScalarOrSequence("demands") select dep.AssertString("dep").Value).ToArray();
                break;
            }
        }
        return this;
    }

    public DictionaryContextData ToContextData() {
        var pool = new DictionaryContextData();
        if(Name?.Length > 0) {
            pool["name"] = new StringContextData(Name);
        }
        if(VmImage?.Length > 0) {
            pool["vmImage"] = new StringContextData(VmImage);
        }
        if(Demands != null) {
            var demands = new ArrayContextData();
            foreach(var dep in Demands) {
                demands.Add(new StringContextData(dep));
            }
            pool["demands"] = demands;
        }
        return pool;
    }
}
}