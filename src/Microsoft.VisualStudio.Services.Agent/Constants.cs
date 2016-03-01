using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public static class Constants
    {
        public static class Agent
        {
            public static readonly int MaxParallelism = 1;
            public static readonly string Version = "2.98.0";    
        }
        
        
        public static class Variables
        {
            public static class Agent
            {
                //
                // Keep alphabetical
                //
                public static readonly string HomeDirectory = "agent.homedirectory";                                
                public static readonly string Id = "agent.id";
                public static readonly string JobName = "agent.jobname";
                public static readonly string JobStatus = "agent.jobstatus";        
                public static readonly string MachineName = "agent.machinename";
                public static readonly string Name = "agent.name";
                public static readonly string OS = "agent.os";
                public static readonly string OSVersion = "agent.osversion";
                public static readonly string RootFolder = "agent.RootDirectory";
                public static readonly string ServerOMFolder = "agent.ServerOMDirectory";
                public static readonly string WorkFolder = "agent.workfolder";
                public static readonly string WorkingFolder = "agent.WorkingDirectory";
            }
            
            public static class Common
            {
                public static readonly string TestResultsDirectory = "common.testresultsdirectory";
            }
            
            public static class System
            {
                //
                // Keep alphabetical
                //
                public static readonly string AccessToken = "system.accessToken";                
                public static readonly string CollectionId = "system.collectionid";
                public static readonly string Debug = "system.debug";
                public static readonly string DefaultWorkingDirectory = "system.defaultworkingdirectory";
                public static readonly string DefinitionId = "system.definitionid";
                public static readonly string EnableAccessToken = "system.enableAccessToken";
                public static readonly string HostType = "system.hosttype";
                // public static readonly string System = "system";
                public static readonly string TeamProject = "system.teamproject";
                // back comapt variable, do not document
                public static readonly string TFServerUrl = "system.TeamFoundationServerUri";
                

                
                


                public static readonly string PreferPowerShellExecution = "system.preferps";
                public static readonly string PreferGit = "system.prefergit";                               
            }
            
            public static class Task
            {
                //
                // Keep alphabetical
                //
                public static readonly string TaskDisplayName = "Task.DisplayName";                   
            }
        }
    }
}