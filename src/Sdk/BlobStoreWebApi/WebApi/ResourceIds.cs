using System;
using GitHub.Services.WebApi;

namespace GitHub.Services.BlobStore.WebApi
{
    public static class ResourceIds
    {
        public static readonly ApiResourceVersion ApiVersion = new ApiResourceVersion("1.0");

        public const string BlobAreaId = "5294EF93-12A1-4D13-8671-9D9D014072C8";
        public const string BlobArea = "blob";

        public const string BlobResourceName = "blobs";
        public const string BlobUrlResourceName = "url";
        public const string BlobBatchResourceName = "blobsbatch";
        public const string ReferenceBatchResourceName = "referencesbatch";

        public static readonly Guid BlobResourceId = new Guid("{D56223DF-8CCD-45C9-89B4-EDDF69240690}");
        public static readonly Guid BlobUrlResourceId = new Guid("{1D1857E7-3F76-4766-AC71-C443AB9093EF}");
        public static readonly Guid ReferenceResourceId = new Guid("{8A483D58-09D3-47D5-8D9F-10DA1BDE704C}");
        public static readonly Guid BlobBatchResourceId = new Guid("{4585DE0E-2CFD-438E-A824-DA53AC2EF0D0}");
        public static readonly Guid ReferenceBatchResourceId = new Guid("{3E5ABB16-5C5B-4C87-9D73-88CB68FA9509}");

        public const string DedupAreaId = "01E4817C-857E-485C-9401-0334A33200DA";
        public const string DedupArea = "dedup";

        public const string ChunkResourceName = "chunks";
        public const string EchoResourceName = "echo";
        public const string NodeResourceName = "nodes";
        public const string DedupUrlsResourceName = "urls";
        public const string DedupUrlsBatchResourceName = "urlsbatch";
        public const string RootResourceName = "roots";
        public const string ValidationResourceName = "validations";

        public static readonly Guid ChunkResourceId = new Guid("{C8911095-CE13-48E9-B1DC-158C716AA6BA}");
        public static readonly Guid NodeResourceId = new Guid("{53E6E1E0-7444-47EA-93CD-44E6DDF264E6}");
        public static readonly Guid EchoResourceId = new Guid("{40213C1A-2CA1-401F-8A49-E0F59668DFDE}");
        public static readonly Guid DedupUrlsResourceId = new Guid("{3C7526CF-A472-4D43-A44D-2B6D98488ECA}");
        public static readonly Guid DedupUrlsBatchResourceId = new Guid("{89D5AC43-8380-4834-B07E-39E26F441D47}");
        public static readonly Guid RootResourceId = new Guid("{B30D4D8E-D3D7-4C2E-9BB5-F1878D6D2E3A}");
        public static readonly Guid ValidationResourceId = new Guid("{2F154496-2068-4F99-8887-B39B1FB8611D}");


        // Client Tools (currently ArtifactTool)
        public const string ClientToolsAreaId = "3FDA18BA-DFF2-42E6-8D10-C521B23B85FC";
        public const string ClientToolsArea = "clienttools";
        public const string ClientToolsReleaseResourceName = "release";
        public static readonly Guid ClientToolsReleaseResourceId = new Guid("187EC90D-DD1E-4EC6-8C57-937D979261E5");

        // Client Telemetry
        public const string TelemetryAreaId = "7670AA71-46BD-4133-BD39-213FF359D30E";
        public const string TelemetryArea = "pipelineartifactstelemetry";
        public const string TelemetryResourceName = "aiinstrumentationkey";
        public static readonly Guid TelemetryResourceId = new Guid("4CAFE3EF-2526-4AAD-A636-204EE8D2F66B");

        // Usage Area
        public const string UsageInfoAreaId = "66939471-964E-4475-9EC2-A616D9BD7522";
        public const string UsageInfoArea = "usage";
        public const string UsageInfoMetricsResource = "metrics";
        public static readonly Guid UsageInfoMetricsResourceId = new Guid("{110C51C8-1A45-4CBF-AC4B-B9B7C1F375ED}");
    }
}
