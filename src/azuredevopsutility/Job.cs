using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;

public class Job {
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string[] DependsOn { get; set; }
    public string Condition { get; set; }
    public Strategy Strategy { get; set; }
    public bool ContinueOnError { get; set; }
    public string Container { get; set; }
    public int TimeoutInMinutes { get; set; }
    public int CancelTimeoutInMinutes { get; set; }
    public Dictionary<string, string> Variables { get; set; }
    public Dictionary<string, string> Services { get; set; }
    public List<JobStep> Steps { get; set; }
    public TemplateToken TemplateContext { get; set; }

    public Job Parse(IFileProvider provider, TemplateToken source, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        var jobToken = source.AssertMapping("job-root");
        foreach(var kv in jobToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "job":
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
                case "strategy":
                    Strategy = new Strategy();
                    foreach(var sv in kv.Value.AssertMapping("strategy")) {
                        switch(sv.Key.AssertString("key").Value) {
                            case "parallel":
                               Strategy.Parallel = (int)kv.Value.AssertNumber("parallel").Value;
                            break;
                            case "Matrix":
                                Strategy.Matrix = sv.Value.AssertMapping("matrix").ToDictionary(kv => kv.Key.AssertString("mk").Value, kv => kv.Value.AssertMapping("matrix").ToDictionary(kv => kv.Key.AssertString("mk").Value, kv => kv.Value.AssertString("mk").Value));
                            break;
                            case "maxParallel":
                               Strategy.MaxParallel = (int)kv.Value.AssertNumber("maxParallel").Value;
                            break;
                        }
                    }
                break;
                case "continueOnError":
                    ContinueOnError = kv.Value.AssertBoolean("continueOnError").Value;
                break;
                case "container":
                    Container = kv.Value.AssertString("container").Value;
                break;
                case "timeoutInMinutes":
                    TimeoutInMinutes = (int)kv.Value.AssertNumber("timeoutInMinutes").Value;
                break;
                case "cancelTimeoutInMinutes":
                    CancelTimeoutInMinutes = (int)kv.Value.AssertNumber("cancelTimeoutInMinutes").Value;
                break;
                case "variables":
                    Variables = kv.Value.AssertMapping("variables").ToDictionary(kv => kv.Key.AssertString("variables").Value, kv => kv.Value.AssertString("variables").Value);
                break;
                case "services":
                    Services = kv.Value.AssertMapping("services").ToDictionary(kv => kv.Key.AssertString("services").Value, kv => kv.Value.AssertString("services").Value);
                break;
                case "steps":
                    Steps = new List<JobStep>();
                    foreach(var step2 in kv.Value.AssertSequence("")) {
                        AzureDevops.ParseSteps(Steps, step2, tasksByNameAndVersion, provider);
                    }
                break;
                case "templateContext":
                    TemplateContext = kv.Value;
                break;
            }
        }
        return this;
    }

    public static void ParseJobs(List<Job> jobs, SequenceToken jobsToken, IFileProvider provider, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        foreach(var job in jobsToken) {
            if(job is MappingToken mstep && mstep.Count > 0) {
                if((mstep[0].Key as StringToken)?.Value == "template") {
                    var path = (mstep[0].Value as StringToken)?.Value;
                    var file = AzureDevops.ReadTemplate(provider, path, mstep.Count == 2 ? mstep[1].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null);
                    ParseJobs(jobs, (from e in file where e.Key.AssertString("").Value == "jobs" select e.Value).First().AssertSequence(""), provider, tasksByNameAndVersion);
                } else {
                    jobs.Add(new Job().Parse(provider, mstep, tasksByNameAndVersion));
                }
            }
        }
    }
}