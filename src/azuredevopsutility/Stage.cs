using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;

public class Stage {
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string[] DependsOn { get; set; }
    public string Condition { get; set; }
    public List<Job> Jobs { get; set; }
    public Dictionary<string, string> Variables { get; set; }
    public TemplateToken TemplateContext { get; set; }

    public Stage Parse(IFileProvider provider, TemplateToken source, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        var jobToken = source.AssertMapping("job-root");
        foreach(var kv in jobToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "stage":
                    Name = kv.Value.AssertString("name").Value;
                break;
                case "displayName":
                    DisplayName = kv.Value.AssertString("name").Value;
                break;
                case "dependsOn":
                    //DependsOn = kv.Value.AssertString("name").Value;
                break;
                case "Condition":
                    Condition = kv.Value.AssertString("condition").Value;
                break;
                case "variables":
                    Variables = kv.Value.AssertMapping("variables").ToDictionary(kv => kv.Key.AssertString("variables").Value, kv => kv.Value.AssertString("variables").Value);
                break;
                case "jobs":
                    Jobs = new List<Job>();
                    Job.ParseJobs(Jobs, kv.Value.AssertSequence(""), provider, tasksByNameAndVersion);
                break;
                case "templateContext":
                    TemplateContext = kv.Value;
                break;
            }
        }
        return this;
    }

    public static void ParseStages(List<Stage> stages, SequenceToken stagesToken, IFileProvider provider, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        foreach(var job in stagesToken) {
            if(job is MappingToken mstep && mstep.Count > 0) {
                if((mstep[0].Key as StringToken)?.Value == "template") {
                    var path = (mstep[0].Value as StringToken)?.Value;
                    var file = AzureDevops.ReadTemplate(provider, path, mstep.Count == 2 ? mstep[1].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null);
                    ParseStages(stages, (from e in file where e.Key.AssertString("").Value == "stages" select e.Value).First().AssertSequence(""), provider, tasksByNameAndVersion);
                } else {
                    stages.Add(new Stage().Parse(provider, mstep, tasksByNameAndVersion));
                }
            }
        }
    }
}