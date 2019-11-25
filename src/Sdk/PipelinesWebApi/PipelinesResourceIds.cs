using System;

namespace GitHub.Actions.Pipelines.WebApi
{
    public static class PipelinesArea
    {
        public const string Name = "pipelines";
        public const string IdString = "2e0bf237-8973-4ec9-a581-9c3d679d1776";
        public static readonly Guid Id = new Guid(PipelinesArea.IdString);
    }

    public static class PipelinesResources
    {
        public static class Artifacts
        {
            public const string Name = "artifacts";
            public static readonly Guid Id = new Guid("85023071-BD5E-4438-89B0-2A5BF362A19D");
        }

        public static class PipelineOrgs
        {
            public const string Name = "orgs";
            public static readonly Guid Id = new Guid("CD70BA1A-D59A-4E0B-9934-97998159CCC8");
        }

        public static class Logs
        {
            public const string Name = "logs";
            public static readonly Guid Id = new Guid("fb1b6d27-3957-43d5-a14b-a2d70403e545");
        }

        public static class Pipelines
        {
            public const string Name = "pipelines";
            public static readonly Guid Id = new Guid("28e1305e-2afe-47bf-abaf-cbb0e6a91988");
        }

        public static class Reputations
        {
            public const string Name = "reputations";
            public static readonly Guid Id = new Guid("ABA353B0-46FB-4885-88C5-391C6B6382B3");
        }

        public static class Runs
        {
            public const string Name = "runs";
            public static readonly Guid Id = new Guid("7859261e-d2e9-4a68-b820-a5d84cc5bb3d");
        }

        public static class SignalR
        {
            public const string Name = "signalr";
            public static readonly Guid Id = new Guid("1FFE4916-AC72-4566-ADD0-9BAB31E44FCF");
        }

        public static class SignedArtifactsContent
        {
            public const string Name = "signedartifactscontent";
            public static readonly Guid Id = new Guid("6B2AC16F-CD00-4DF9-A13B-3A1CC8AFB188");
        }

        public static class SignedLogContent
        {
            public const string Name = "signedlogcontent";
            public static readonly Guid Id = new Guid("74f99e32-e2c4-44f4-93dc-dec0bca530a5");
        }

        public static class SignalRLive
        {
            public const string Name = "live";
            public static readonly Guid Id = new Guid("C41B3775-6D50-48BD-B261-42DA7F0F1BA0");
        }
    }
}
