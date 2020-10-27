using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class ServiceInterfacesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void RunnerInterfacesSpecifyDefaultImplementation()
        {
            // Validate all interfaces in the Listener assembly define a valid service locator attribute.
            // Otherwise, the interface needs to whitelisted.
            var whitelist = new[]
            {
                typeof(ICredentialProvider)
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
                typeof(IRunnerService),
                typeof(ICredentialProvider),
                typeof(IExtension),
                typeof(IHostContext),
                typeof(ITraceManager),
                typeof(IThrottlingReporter),
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
                typeof(IActionCommandExtension),
                typeof(IExecutionContext),
                typeof(IFileCommandExtension),
                typeof(IHandler),
                typeof(IJobExtension),
                typeof(IStep),
                typeof(IStepHost),
                typeof(IDiagnosticLogManager),
                typeof(IEnvironmentContextData)
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
                // Temporary hack due to shared code copied in two places.
                if (interfaceTypeInfo.FullName.StartsWith("GitHub.DistributedTask"))
                {
                    continue;
                }

                if (interfaceTypeInfo.FullName.Contains("IConverter")){
                    continue;
                }

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
