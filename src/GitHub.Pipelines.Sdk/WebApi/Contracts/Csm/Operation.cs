using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Container for a list of operations supported by a resource provider.
    /// </summary>
    public class OperationListResult
    {
        public OperationListResult()
        {
            value = new List<Operation>();
        }

        /// <summary>
        /// A list of operations supported by a resource provider.
        /// </summary>
        public List<Operation> value { get; set; }
    }

    /// <summary>
    /// Properties of an operation supported by the resource provider.
    /// </summary>
    public class Operation
    {
        private OperationProperties displayProperties;

        public Operation(string provider, string resource, string operation, string description)
        {
            displayProperties = new OperationProperties(provider, resource, operation, description);
        }

        /// <summary>
        /// The name of the resource operation.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The properties of the resource operation.
        /// </summary>
        public OperationProperties display
        {
            get
            {
                return displayProperties;
            }
            set
            {
                value = displayProperties;
            }
        }

        public static Operation GetOperationDescriptorForRPandAction(ResourceProvider resourceProvider, OperationAction action)
        {
            return GetOperationDescriptorForRPandAction(resourceProvider, ResourceProvider.None, action);
        }

        public static Operation GetOperationDescriptorForRPandAction(ResourceProvider rootResourceProvider, ResourceProvider childResourceProvider, OperationAction action)
        {
            const string name = "Microsoft.VisualStudio/{0}/{1}";
            const string provider = "Visual Studio";
            string description;
            string operationText;

            string resourceProvider = string.Empty;

            if (childResourceProvider != ResourceProvider.None)
            {
                resourceProvider = string.Format("{0}/{1}", rootResourceProvider, childResourceProvider);
            }
            else
            {
                resourceProvider = rootResourceProvider.ToString();
            }

            // Building description string in format of 'Microsoft.Logic/workflows/read'
            // Building operation string in format of 'Set Workflow'
            switch (action)
            {
                case OperationAction.Write:
                    description = $"Creates or updates the {resourceProvider}";
                    operationText = $"Set {resourceProvider}";
                    break;
                case OperationAction.Delete:
                    description = $"Deletes the {resourceProvider}";
                    operationText = $"Delete {resourceProvider}";
                    break;
                case OperationAction.Read:
                    description = $"Reads the {resourceProvider}";
                    operationText = $"Read {resourceProvider}";
                    break;
                case OperationAction.Action:
                    description = $"Registers the Azure Subscription with Microsoft.VisualStudio provider";
                    operationText = $"Register Azure Subscription with Microsoft.VisualStudio provider";
                    break;
                default:
                    description = string.Empty;
                    operationText = string.Empty;
                    break;
            }

            var operation = new Operation(provider, resourceProvider, description, operationText)
            {
                name = string.Format(name, action == OperationAction.Action ? "Register" : resourceProvider, action)
            };
            return operation;
        }
    }

    /// <summary>
    /// Properties of an operation supported by the resource provider.
    /// </summary>
    public struct OperationProperties
    {
        /// <summary>
        /// The provider name.
        /// </summary>
        public string provider;
        /// <summary>
        /// The resource name.
        /// </summary>
        public string resource;
        /// <summary>
        /// The operation name.
        /// </summary>
        public string operation;
        /// <summary>
        /// The description of the resource operation.
        /// </summary>
        public string description;

        public OperationProperties(string providerStr, string resourceStr, string operationStr, string descriptionStr)
        {
            this.provider = providerStr;
            this.resource = resourceStr;
            this.operation = operationStr;
            this.description = descriptionStr;
        }
    }

    public enum OperationAction
    {
        Write,
        Delete,
        Read,
        Action
    }

    public enum ResourceProvider
    {
        Account,
        Project,
        Extension,
        None
    }
}
