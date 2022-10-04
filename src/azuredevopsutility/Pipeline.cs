using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;

public class Pipeline {
    public string Name { get; set; }
    public List<Stage> Stages { get; set; }
    public Dictionary<string, string> Variables { get; set; }
    public TemplateToken TemplateContext { get; set; }

    public Pipeline Parse(IFileProvider provider, TemplateToken source, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        var jobToken = source.AssertMapping("job-root");
        foreach(var kv in jobToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "name":
                    Name = kv.Value.AssertString("name").Value;
                break;
                case "variables":
                    Variables = kv.Value.AssertMapping("variables").ToDictionary(kv => kv.Key.AssertString("variables").Value, kv => kv.Value.AssertString("variables").Value);
                break;
                case "stages":
                    Stages = new List<Stage>();
                    Stage.ParseStages(Stages, kv.Value.AssertSequence(""), provider, tasksByNameAndVersion);
                break;
                case "templateContext":
                    TemplateContext = kv.Value;
                break;
            }
        }
        return this;
    }
}