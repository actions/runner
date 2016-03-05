using Microsoft.VisualStudio.Services.Agent.Configuration;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ServiceInterfacesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void AgentInterfacesSpecifyDefaultImplementation()
        {
            Validate(typeof(IMessageListener).GetTypeInfo().Assembly, 
                    // whitelist 
                    typeof(ICredentialProvider));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CommonInterfacesSpecifyDefaultImplementation()
        {
            Validate(
                typeof(IHostContext).GetTypeInfo().Assembly, // assembly
                typeof(IHostContext), // whitelist params
                typeof(IAgentService),
                typeof(ICredentialProvider),
                typeof(ITraceManager),
                typeof(IExtension),
                typeof(ILogWriter),
                typeof(IVariables),
                typeof(IVariablesExtension),
                typeof(ICommandExtension));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void WorkerInterfacesSpecifyDefaultImplementation()
        {
            Validate(
                typeof(IStepsRunner).GetTypeInfo().Assembly, // assembly
                typeof(IExecutionContext), // whitelist params
                typeof(IJobExtension),
                typeof(IStep));
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
