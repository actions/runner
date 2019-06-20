using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(TrackingManager))]
    public interface ITrackingManager : IRunnerService
    {
        TrackingConfig Create(IExecutionContext executionContext, string file);

        TrackingConfig LoadIfExists(IExecutionContext executionContext, string file);

        void Update(IExecutionContext executionContext, TrackingConfig config, string file);
    }

    public sealed class TrackingManager : RunnerService, ITrackingManager
    {
        public TrackingConfig Create(
            IExecutionContext executionContext,
            string file)
        {
            Trace.Entering();

            // Create the new tracking config.
            TrackingConfig config = new TrackingConfig(executionContext);
            WriteToFile(file, config);
            return config;
        }

        public TrackingConfig LoadIfExists(
            IExecutionContext executionContext,
            string file)
        {
            Trace.Entering();

            // The tracking config will not exist for a new definition.
            if (!File.Exists(file))
            {
                return null;
            }

            return IOUtil.LoadObject<TrackingConfig>(file);
        }

        public void Update(
            IExecutionContext executionContext,
            TrackingConfig config,
            string file)
        {
            Trace.Entering();
            WriteToFile(file, config);
        }

        private void WriteToFile(string file, object value)
        {
            Trace.Entering();
            Trace.Verbose($"Writing config to file: {file}");

            // Create the directory if it does not exist.
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            IOUtil.SaveObject(value, file);
        }
    }
}
