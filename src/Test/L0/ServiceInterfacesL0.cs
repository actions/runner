using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Listener.Capabilities;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ServiceInterfacesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void AgentInterfacesSpecifyDefaultImplementation()
        {
            // Validate all interfaces in the Listener assembly define a valid service locator attribute.
            // Otherwise, the interface needs to whitelisted.
            var whitelist = new[]
            {
                typeof(ICapabilitiesProvider),
                typeof(ICredentialProvider),
            };
            Validate(
                assembly: typeof(IMessageListener).GetTypeInfo().Assembly,
                whitelist: whitelist);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CommonInterfacesSpecifyDefaultImplementation()
        {
            // Validate all interfaces in the Common assembly define a valid service locator attribute.
            // Otherwise, the interface needs to whitelisted.
            var whitelist = new[]
            {
                typeof(IAgentService),
                typeof(ICredentialProvider),
                typeof(IExtension),
                typeof(IHostContext),
                typeof(ITraceManager),
                typeof(ISecret)
            };
            Validate(
                assembly: typeof(IHostContext).GetTypeInfo().Assembly,
                whitelist: whitelist);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void WorkerInterfacesSpecifyDefaultImplementation()
        {
            // Validate all interfaces in the Worker assembly define a valid service locator attribute.
            // Otherwise, the interface needs to whitelisted.
            var whitelist = new[]
            {
                typeof(IArtifactDetails),
                typeof(IArtifactExtension),
                typeof(ICodeCoverageEnabler),
                typeof(ICodeCoverageSummaryReader),
                typeof(IExecutionContext),
                typeof(IHandler),
                typeof(IJobExtension),
                typeof(IResultReader),
                typeof(ISourceProvider),
                typeof(IStep),
                typeof(ITfsVCMapping),
                typeof(ITfsVCPendingChange),
                typeof(ITfsVCShelveset),
                typeof(ITfsVCStatus),
                typeof(ITfsVCWorkspace),
                typeof(IWorkerCommandExtension),
            };
            Validate(
                assembly: typeof(IStepsRunner).GetTypeInfo().Assembly,
                whitelist: whitelist);
        }

        private static void Validate(Assembly assembly, params Type[] whitelist)
        {
            // Iterate over all non-whitelisted interfaces contained within the assembly.
            IDictionary<TypeInfo, Type> w = whitelist.ToDictionary(x => x.GetTypeInfo());
            foreach (TypeInfo interfaceTypeInfo in assembly.DefinedTypes.Where(x => x.IsInterface && !w.ContainsKey(x)))
            {
                // Assert the ServiceLocatorAttribute is defined on the interface.
                CustomAttributeData attribute =
                    interfaceTypeInfo
                    .CustomAttributes
                    .SingleOrDefault(x => x.AttributeType == typeof(ServiceLocatorAttribute));
                Assert.True(attribute != null, $"Missing {nameof(ServiceLocatorAttribute)} for interface '{interfaceTypeInfo.FullName}'. Add the attribute to the interface or whitelist the interface in the test.");

                // Assert the interface is mapped to a concrete type.
                CustomAttributeNamedArgument defaultArg =
                    attribute
                    .NamedArguments
                    .SingleOrDefault(x => String.Equals(x.MemberName, ServiceLocatorAttribute.DefaultPropertyName, StringComparison.Ordinal));
                Type concreteType = defaultArg.TypedValue.Value as Type;
                string invalidConcreteTypeMessage = $"Invalid Default parameter on {nameof(ServiceLocatorAttribute)} for the interface '{interfaceTypeInfo.FullName}'. The default implementation must not be null, must not be an interface, must be a class, and must implement the interface '{interfaceTypeInfo.FullName}'.";
                Assert.True(concreteType != null, invalidConcreteTypeMessage);
                TypeInfo concreteTypeInfo = concreteType.GetTypeInfo();
                Assert.False(concreteTypeInfo.IsInterface, invalidConcreteTypeMessage);
                Assert.True(concreteTypeInfo.IsClass, invalidConcreteTypeMessage);
                Assert.True(concreteTypeInfo.ImplementedInterfaces.Any(x => x.GetTypeInfo() == interfaceTypeInfo), invalidConcreteTypeMessage);
            }
        }
    }
}
