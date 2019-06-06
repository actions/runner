using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DistributedTaskException", "GitHub.DistributedTask.WebApi.DistributedTaskException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DistributedTaskException : VssServiceException
    {
        public DistributedTaskException(String message)
            : base(message)
        {
        }

        public DistributedTaskException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DistributedTaskException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ServerJobFailureException : DistributedTaskException
    {
        public ServerJobFailureException(String message)
            : base(message)
        {
        }

        public ServerJobFailureException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ServerJobFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidTaskExecutionModeTypeException : DistributedTaskException
    {
        public InvalidTaskExecutionModeTypeException(String message)
            : base(message)
        {
        }

        public InvalidTaskExecutionModeTypeException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidTaskExecutionModeTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ServerExecutionHandlerNotFoundException : DistributedTaskException
    {
        public ServerExecutionHandlerNotFoundException(String message)
            : base(message)
        {
        }

        public ServerExecutionHandlerNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ServerExecutionHandlerNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskJsonNotFoundException", "GitHub.DistributedTask.WebApi.TaskJsonNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidBuildContributionsTarget : DistributedTaskException
    {
        public InvalidBuildContributionsTarget(String message)
            : base(message)
        {
        }

        public InvalidBuildContributionsTarget(String message, Exception ex)
            : base(message, ex)
        {
        }
        protected InvalidBuildContributionsTarget(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "SecurityException", "GitHub.DistributedTask.WebApi.SecurityException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class SecurityException : DistributedTaskException
    {
        public SecurityException(String message)
            : base(message)
        {
        }

        public SecurityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AccessDeniedException", "GitHub.DistributedTask.WebApi.AccessDeniedException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccessDeniedException : SecurityException
    {
        public AccessDeniedException(String message)
            : base(message)
        {
        }

        public AccessDeniedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AccessDeniedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DataNotFoundException", "GitHub.DistributedTask.WebApi.DataNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DataNotFoundException : DistributedTaskException
    {
        public DataNotFoundException(String message)
            : base(message)
        {
        }

        public DataNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected DataNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "DataSourceNotFoundException", "GitHub.DistributedTask.WebApi.DataSourceNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class DataSourceNotFoundException : DistributedTaskException
    {
        public DataSourceNotFoundException(string message) : base(message)
        {
        }

        public DataSourceNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataSourceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidDataSourceBindingException", "GitHub.DistributedTask.WebApi.InvalidDataSourceBindingException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidDataSourceBindingException : DistributedTaskException
    {
        public InvalidDataSourceBindingException(string message) : base(message)
        {
        }

        public InvalidDataSourceBindingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidDataSourceBindingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidServiceEndpointRequestException", "GitHub.DistributedTask.WebApi.InvalidServiceEndpointRequestException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidServiceEndpointRequestException : DistributedTaskException
    {
        public InvalidServiceEndpointRequestException(string message) : base(message)
        {
        }

        public InvalidServiceEndpointRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidServiceEndpointRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "EndpointNotFoundException", "GitHub.DistributedTask.WebApi.EndpointNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class EndpointNotFoundException : DistributedTaskException
    {
        public EndpointNotFoundException(String message)
            : base(message)
        {
        }

        public EndpointNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EndpointNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidJsonPathResponseSelectorException : DistributedTaskException
    {
        public InvalidJsonPathResponseSelectorException(string message)
            : base(message)
        {
        }

        public InvalidJsonPathResponseSelectorException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidJsonPathResponseSelectorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidPackageQueryException : DistributedTaskException
    {
        public InvalidPackageQueryException(string message)
            : base(message)
        {
        }

        public InvalidPackageQueryException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidPackageQueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidAuthorizationDetailsException", "GitHub.DistributedTask.WebApi.InvalidAuthorizationDetailsException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidAuthorizationDetailsException : DistributedTaskException
    {
        public InvalidAuthorizationDetailsException(string message) : base(message)
        {
        }

        public InvalidAuthorizationDetailsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidAuthorizationDetailsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidEndpointResponseException", "GitHub.DistributedTask.WebApi.InvalidEndpointResponseException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidEndpointResponseException : DistributedTaskException
    {
        public InvalidEndpointResponseException(string message) : base(message)
        {
        }

        public InvalidEndpointResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidEndpointResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidTaskAgentPoolException : DistributedTaskException
    {
        public InvalidTaskAgentPoolException(string message) : base(message)
        {
        }

        public InvalidTaskAgentPoolException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidTaskAgentPoolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidDeploymentMachineException : DistributedTaskException
    {
        public InvalidDeploymentMachineException(string message) : base(message)
        {
        }

        public InvalidDeploymentMachineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidDeploymentMachineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class MetaTaskDefinitionExistsException : DistributedTaskException
    {
        public MetaTaskDefinitionExistsException(String message)
            : base(message)
        {
        }

        public MetaTaskDefinitionExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private MetaTaskDefinitionExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskGroupDraftExistsException : DistributedTaskException
    {
        public TaskGroupDraftExistsException(String message)
            : base(message)
        {
        }

        public TaskGroupDraftExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskGroupDraftExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskGroupPreviewExistsException : DistributedTaskException
    {
        public TaskGroupPreviewExistsException(String message)
            : base(message)
        {
        }

        public TaskGroupPreviewExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskGroupPreviewExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskGroupDisabledException : DistributedTaskException
    {
        public TaskGroupDisabledException(String message)
            : base(message)
        {
        }

        public TaskGroupDisabledException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskGroupDisabledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskGroupIdConflictException : DistributedTaskException
    {
        public TaskGroupIdConflictException(String message)
            : base(message)
        {
        }

        public TaskGroupIdConflictException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskGroupIdConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class MetaTaskDefinitionNotFoundException : DistributedTaskException
    {
        public MetaTaskDefinitionNotFoundException(String message)
            : base(message)
        {
        }

        public MetaTaskDefinitionNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private MetaTaskDefinitionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class MetaTaskDefinitionRunsOnMismatchException : DistributedTaskException
    {
        public MetaTaskDefinitionRunsOnMismatchException(String message)
            : base(message)
        {
        }

        public MetaTaskDefinitionRunsOnMismatchException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private MetaTaskDefinitionRunsOnMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class InvalidTaskDefinitionTypeException : DistributedTaskException
    {
        public InvalidTaskDefinitionTypeException(String message)
            : base(message)
        {
        }

        public InvalidTaskDefinitionTypeException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidTaskDefinitionTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class PackageExistsException : DistributedTaskException
    {
        public PackageExistsException(string message)
            : base(message)
        {
        }

        public PackageExistsException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected PackageExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "PackageNotFoundException", "GitHub.DistributedTask.WebApi.PackageNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class PackageNotFoundException : DistributedTaskException
    {
        public PackageNotFoundException(String message)
            : base(message)
        {
        }

        public PackageNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private PackageNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "PackagePropertyUnknownException", "GitHub.DistributedTask.WebApi.PackagePropertyUnknownException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class PackagePropertyUnknownException : DistributedTaskException
    {
        public PackagePropertyUnknownException(String message)
            : base(message)
        {
        }

        public PackagePropertyUnknownException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private PackagePropertyUnknownException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class PackageVersionInvalidException : DistributedTaskException
    {
        public PackageVersionInvalidException(string message)
            : base(message)
        {
        }

        public PackageVersionInvalidException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected PackageVersionInvalidException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ServiceEndpointException : DistributedTaskException
    {
        public ServiceEndpointException(string message) : base(message)
        {
        }

        public ServiceEndpointException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ServiceEndpointException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class OAuthConfigurationException : DistributedTaskException
    {
        public OAuthConfigurationException(string message) : base(message)
        {
        }

        public OAuthConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OAuthConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ServiceEndpointNotFoundException", "GitHub.DistributedTask.WebApi.ServiceEndpointNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ServiceEndpointNotFoundException : DistributedTaskException
    {
        public ServiceEndpointNotFoundException(String message)
            : base(message)
        {
        }

        public ServiceEndpointNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ServiceEndpointNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ServiceEndpointQueryFailedException", "GitHub.DistributedTask.WebApi.ServiceEndpointQueryFailedException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ServiceEndpointQueryFailedException : DistributedTaskException
    {
        public ServiceEndpointQueryFailedException(String message)
            : base(message)
        {
        }

        public ServiceEndpointQueryFailedException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ServiceEndpointQueryFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ServiceEndpointUntrustedHostException : DistributedTaskException
    {
        public ServiceEndpointUntrustedHostException(string message)
            : base(message)
        {
        }

        public ServiceEndpointUntrustedHostException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected ServiceEndpointUntrustedHostException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentExistsException", "GitHub.DistributedTask.WebApi.TaskAgentExistsException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TaskAgentExistsException : DistributedTaskException
    {
        public TaskAgentExistsException(String message)
            : base(message)
        {
        }

        public TaskAgentExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TaskAgentExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentJobFailedNotEnoughSubscriptionResourcesException", "GitHub.DistributedTask.WebApi.TaskAgentJobFailedNotEnoughSubscriptionResourcesException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentJobFailedNotEnoughSubscriptionResourcesException : DistributedTaskException
    {
        public TaskAgentJobFailedNotEnoughSubscriptionResourcesException(String message)
            : base(message)
        {
        }

        public TaskAgentJobFailedNotEnoughSubscriptionResourcesException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentJobFailedNotEnoughSubscriptionResourcesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentJobNotFoundException", "GitHub.DistributedTask.WebApi.TaskAgentJobNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentJobNotFoundException : DistributedTaskException
    {
        public TaskAgentJobNotFoundException(String message)
            : base(message)
        {
        }

        public TaskAgentJobNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentJobNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentJobStillRunningException", "GitHub.DistributedTask.WebApi.TaskAgentJobStillRunningException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentJobStillRunningException : DistributedTaskException
    {
        public TaskAgentJobStillRunningException(String message)
            : base(message)
        {
        }

        public TaskAgentJobStillRunningException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentJobStillRunningException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentJobTokenExpiredException", "GitHub.DistributedTask.WebApi.TaskAgentJobTokenExpiredException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentJobTokenExpiredException : DistributedTaskException
    {
        public TaskAgentJobTokenExpiredException(String message)
            : base(message)
        {
        }

        public TaskAgentJobTokenExpiredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentJobTokenExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentNotFoundException", "GitHub.DistributedTask.WebApi.TaskAgentNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentNotFoundException : DistributedTaskException
    {
        public TaskAgentNotFoundException(String message)
            : base(message)
        {
        }

        public TaskAgentNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentVersionNotSupportedException", "GitHub.DistributedTask.WebApi.TaskAgentVersionNotSupportedException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentVersionNotSupportedException : DistributedTaskException
    {
        public TaskAgentVersionNotSupportedException(String message)
            : base(message)
        {
        }

        public TaskAgentVersionNotSupportedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentVersionNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentPoolExistsException", "GitHub.DistributedTask.WebApi.TaskAgentPoolExistsException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TaskAgentPoolExistsException : DistributedTaskException
    {
        public TaskAgentPoolExistsException(String message)
            : base(message)
        {
        }

        public TaskAgentPoolExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TaskAgentPoolExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentPoolMaintenanceDefinitionNotFoundException", "GitHub.DistributedTask.WebApi.TaskAgentPoolMaintenanceDefinitionNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentPoolMaintenanceDefinitionNotFoundException : DistributedTaskException
    {
        public TaskAgentPoolMaintenanceDefinitionNotFoundException(String message)
            : base(message)
        {
        }

        public TaskAgentPoolMaintenanceDefinitionNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPoolMaintenanceDefinitionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentPoolMaintenanceJobNotFoundException", "GitHub.DistributedTask.WebApi.TaskAgentPoolMaintenanceJobNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentPoolMaintenanceJobNotFoundException : DistributedTaskException
    {
        public TaskAgentPoolMaintenanceJobNotFoundException(String message)
            : base(message)
        {
        }

        public TaskAgentPoolMaintenanceJobNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPoolMaintenanceJobNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentPoolMaintenanceNotEnabledException", "GitHub.DistributedTask.WebApi.TaskAgentPoolMaintenanceNotEnabledException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentPoolMaintenanceNotEnabledException : DistributedTaskException
    {
        public TaskAgentPoolMaintenanceNotEnabledException(String message)
            : base(message)
        {
        }

        public TaskAgentPoolMaintenanceNotEnabledException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPoolMaintenanceNotEnabledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentPendingUpdateExistsException", "GitHub.DistributedTask.WebApi.TaskAgentPendingUpdateExistsException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentPendingUpdateExistsException : DistributedTaskException
    {
        public TaskAgentPendingUpdateExistsException(String message)
            : base(message)
        {
        }

        public TaskAgentPendingUpdateExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPendingUpdateExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentPendingUpdateNotFoundException", "GitHub.DistributedTask.WebApi.TaskAgentPendingUpdateNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentPendingUpdateNotFoundException : DistributedTaskException
    {
        public TaskAgentPendingUpdateNotFoundException(String message)
            : base(message)
        {
        }

        public TaskAgentPendingUpdateNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPendingUpdateNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentPoolNotFoundException", "GitHub.DistributedTask.WebApi.TaskAgentPoolNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentPoolNotFoundException : DistributedTaskException
    {
        public TaskAgentPoolNotFoundException(String message)
            : base(message)
        {
        }

        public TaskAgentPoolNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPoolNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskAgentPoolRemovedException : DistributedTaskException
    {
        public TaskAgentPoolRemovedException(String message)
            : base(message)
        {
        }

        public TaskAgentPoolRemovedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPoolRemovedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskAgentPoolTypeMismatchException : DistributedTaskException
    {
        public TaskAgentPoolTypeMismatchException(String message)
            : base(message)
        {
        }

        public TaskAgentPoolTypeMismatchException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentPoolTypeMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DeploymentPoolInUseException : DistributedTaskException
    {
        public DeploymentPoolInUseException(String message)
            : base(message)
        {
        }

        public DeploymentPoolInUseException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeploymentPoolInUseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentQueueExistsException", "GitHub.DistributedTask.WebApi.TaskAgentQueueExistsException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentQueueExistsException : DistributedTaskException
    {
        public TaskAgentQueueExistsException(String message)
            : base(message)
        {
        }

        public TaskAgentQueueExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentQueueExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentQueueNotFoundException", "GitHub.DistributedTask.WebApi.TaskAgentQueueNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentQueueNotFoundException : DistributedTaskException
    {
        public TaskAgentQueueNotFoundException(String message)
            : base(message)
        {
        }

        public TaskAgentQueueNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentQueueNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DeploymentMachineGroupExistsException : DistributedTaskException
    {
        public DeploymentMachineGroupExistsException(String message)
            : base(message)
        {
        }

        public DeploymentMachineGroupExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeploymentMachineGroupExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class DeploymentGroupException : DistributedTaskException
    {
        public DeploymentGroupException(string message) : base(message)
        {
        }

        public DeploymentGroupException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeploymentGroupException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DeploymentGroupExistsException : DistributedTaskException
    {
        public DeploymentGroupExistsException(String message)
            : base(message)
        {
        }

        public DeploymentGroupExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeploymentGroupExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DeploymentMachineExistsException : DistributedTaskException
    {
        public DeploymentMachineExistsException(String message)
            : base(message)
        {
        }

        public DeploymentMachineExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeploymentMachineExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class RESTEndpointNotSupportedException : DistributedTaskException
    {
        public RESTEndpointNotSupportedException(String message)
            : base(message)
        {
        }

        public RESTEndpointNotSupportedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private RESTEndpointNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DeploymentMachineGroupNotFoundException : DistributedTaskException
    {
        public DeploymentMachineGroupNotFoundException(String message)
            : base(message)
        {
        }

        public DeploymentMachineGroupNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeploymentMachineGroupNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DeploymentGroupNotFoundException : DistributedTaskException
    {
        public DeploymentGroupNotFoundException(String message)
            : base(message)
        {
        }

        public DeploymentGroupNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeploymentGroupNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DeploymentMachineNotFoundException : DistributedTaskException
    {
        public DeploymentMachineNotFoundException(String message)
            : base(message)
        {
        }

        public DeploymentMachineNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeploymentMachineNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class InvalidTaskAgentVersionException : DistributedTaskException
    {
        public InvalidTaskAgentVersionException(String message)
            : base(message)
        {
        }

        public InvalidTaskAgentVersionException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidTaskAgentVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentAccessTokenExpiredException : DistributedTaskException
    {
        public TaskAgentAccessTokenExpiredException(String message)
            : base(message)
        {
        }

        public TaskAgentAccessTokenExpiredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TaskAgentAccessTokenExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentSessionConflictException", "GitHub.DistributedTask.WebApi.TaskAgentSessionConflictException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentSessionConflictException : DistributedTaskException
    {
        public TaskAgentSessionConflictException(String message)
            : base(message)
        {
        }

        public TaskAgentSessionConflictException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentSessionConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskAgentSessionDeletedException : DistributedTaskException
    {
        public TaskAgentSessionDeletedException(String message)
            : base(message)
        {
        }

        public TaskAgentSessionDeletedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentSessionDeletedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskAgentSessionExpiredException", "GitHub.DistributedTask.WebApi.TaskAgentSessionExpiredException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskAgentSessionExpiredException : DistributedTaskException
    {
        public TaskAgentSessionExpiredException(String message)
            : base(message)
        {
        }

        public TaskAgentSessionExpiredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskAgentSessionExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskDefinitionExistsException", "GitHub.DistributedTask.WebApi.TaskDefinitionExistsException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskDefinitionExistsException : DistributedTaskException
    {
        public TaskDefinitionExistsException(String message)
            : base(message)
        {
        }

        public TaskDefinitionExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskDefinitionExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskDefinitionExistsWithHigherVersionException", "GitHub.DistributedTask.WebApi.TaskDefinitionExistsException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskDefinitionExistsWithHigherVersionException : DistributedTaskException
    {
        public TaskDefinitionExistsWithHigherVersionException(String message)
            : base(message)
        {
        }

        public TaskDefinitionExistsWithHigherVersionException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskDefinitionExistsWithHigherVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskDefinitionHostContextMismatchException", "GitHub.DistributedTask.WebApi.TaskDefinitionHostContextMismatchException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskDefinitionHostContextMismatchException : DistributedTaskException
    {
        public TaskDefinitionHostContextMismatchException(String message)
            : base(message)
        {
        }

        public TaskDefinitionHostContextMismatchException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskDefinitionHostContextMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskDefinitionInputRequiredException", "GitHub.DistributedTask.WebApi.TaskDefinitionInputRequiredException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskDefinitionInputRequiredException : DistributedTaskException
    {
        public TaskDefinitionInputRequiredException(String message)
            : base(message)
        {
        }

        public TaskDefinitionInputRequiredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskDefinitionInputRequiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskDefinitionInvalidException", "GitHub.DistributedTask.WebApi.TaskDefinitionInvalidException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskDefinitionInvalidException : DistributedTaskException
    {
        public TaskDefinitionInvalidException(String message)
            : base(message)
        {
        }

        public TaskDefinitionInvalidException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskDefinitionInvalidException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskDefinitionNotFoundException", "GitHub.DistributedTask.WebApi.TaskDefinitionNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskDefinitionNotFoundException : DistributedTaskException
    {
        public TaskDefinitionNotFoundException(String message)
            : base(message)
        {
        }

        public TaskDefinitionNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskDefinitionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskJsonNotFoundException", "GitHub.DistributedTask.WebApi.TaskJsonNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class TaskJsonNotFoundException : DistributedTaskException
    {
        public TaskJsonNotFoundException(String message)
            : base(message)
        {
        }

        public TaskJsonNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskJsonNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskOrchestrationPlanAlreadyStartedException", "GitHub.DistributedTask.WebApi.TaskOrchestrationPlanAlreadyStartedException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskOrchestrationPlanAlreadyStartedException : DistributedTaskException
    {
        public TaskOrchestrationPlanAlreadyStartedException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationPlanAlreadyStartedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationPlanAlreadyStartedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskOrchestrationPlanCanceledException", "GitHub.DistributedTask.WebApi.TaskOrchestrationPlanCanceledException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskOrchestrationPlanCanceledException : DistributedTaskException
    {
        public TaskOrchestrationPlanCanceledException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationPlanCanceledException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationPlanCanceledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskOrchestrationPlanNotFoundException", "GitHub.DistributedTask.WebApi.TaskOrchestrationPlanNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskOrchestrationPlanNotFoundException : DistributedTaskException
    {
        public TaskOrchestrationPlanNotFoundException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationPlanNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationPlanNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskOrchestrationPlanNotFoundException", "GitHub.DistributedTask.WebApi.TaskOrchestrationPlanNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class InvalidLicenseHubException : DistributedTaskException
    {
        public InvalidLicenseHubException(String message)
            : base(message)
        {
        }

        public InvalidLicenseHubException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidLicenseHubException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskOrchestrationJobNotFoundException", "GitHub.DistributedTask.WebApi.TaskOrchestrationJobNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskOrchestrationJobNotFoundException : DistributedTaskException
    {
        public TaskOrchestrationJobNotFoundException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationJobNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationJobNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskOrchestrationPlanSecurityException", "GitHub.DistributedTask.WebApi.TaskOrchestrationPlanSecurityException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskOrchestrationPlanSecurityException : DistributedTaskException
    {
        public TaskOrchestrationPlanSecurityException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationPlanSecurityException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationPlanSecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskOrchestrationPlanTerminatedException", "GitHub.DistributedTask.WebApi.TaskOrchestrationPlanTerminatedException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskOrchestrationPlanTerminatedException : DistributedTaskException
    {
        public TaskOrchestrationPlanTerminatedException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationPlanTerminatedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationPlanTerminatedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TimelineExistsException : DistributedTaskException
    {
        public TimelineExistsException(String message)
            : base(message)
        {
        }

        public TimelineExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TimelineExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TimelineNotFoundException", "GitHub.DistributedTask.WebApi.TimelineNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TimelineNotFoundException : DistributedTaskException
    {
        public TimelineNotFoundException(String message)
            : base(message)
        {
        }

        public TimelineNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TimelineNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TimelineRecordNotFoundException", "GitHub.DistributedTask.WebApi.TimelineRecordNotFoundException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TimelineRecordNotFoundException : DistributedTaskException
    {
        public TimelineRecordNotFoundException(String message)
            : base(message)
        {
        }

        public TimelineRecordNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TimelineRecordNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TimelineRecordUpdateException", "GitHub.DistributedTask.WebApi.TimelineRecordUpdateException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TimelineRecordUpdateException : DistributedTaskException
    {
        public TimelineRecordUpdateException(String message)
            : base(message)
        {
        }

        public TimelineRecordUpdateException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TimelineRecordUpdateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidTaskJsonException", "GitHub.DistributedTask.WebApi.InvalidTaskJsonException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class InvalidTaskJsonException : DistributedTaskException
    {
        public InvalidTaskJsonException(String message)
            : base(message)
        {
        }

        public InvalidTaskJsonException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidTaskJsonException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidTaskDefinitionInputs", "GitHub.DistributedTask.WebApi.InvalidTaskDefinitionInputs, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class InvalidTaskDefinitionInputsException : DistributedTaskException
    {
        public InvalidTaskDefinitionInputsException(String message)
            : base(message)
        {
        }

        public InvalidTaskDefinitionInputsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidTaskDefinitionInputsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidExtensionException", "GitHub.DistributedTask.WebApi.InvalidExtensionException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class InvalidExtensionException : DistributedTaskException
    {
        public InvalidExtensionException(String message)
            : base(message)
        {
        }

        public InvalidExtensionException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidExtensionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ContributionDoesNotTargetBuildTask", "GitHub.DistributedTask.WebApi.ContributionDoesNotTargetBuildTask, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContributionDoesNotTargetBuildTaskException : DistributedTaskException
    {
        public ContributionDoesNotTargetBuildTaskException(String message)
            : base(message)
        {
        }

        public ContributionDoesNotTargetBuildTaskException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private ContributionDoesNotTargetBuildTaskException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ContributionDoesNotTargetServiceEndpointException", "GitHub.DistributedTask.WebApi.ContributionDoesNotTargetServiceEndpointException, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class ContributionDoesNotTargetServiceEndpointException : DistributedTaskException
    {
        public ContributionDoesNotTargetServiceEndpointException(String message)
            : base(message)
        {
        }

        public ContributionDoesNotTargetServiceEndpointException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private ContributionDoesNotTargetServiceEndpointException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskGroupRevisionAlreadyExistsException : DistributedTaskException
    {
        public TaskGroupRevisionAlreadyExistsException(String message)
            : base(message)
        {
        }

        public TaskGroupRevisionAlreadyExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskGroupRevisionAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskGroupAlreadyUpdatedException : DistributedTaskException
    {
        public TaskGroupAlreadyUpdatedException(String message)
            : base(message)
        {
        }

        public TaskGroupAlreadyUpdatedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskGroupAlreadyUpdatedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class ExtensionIsPublicAndHasPipelineDecoratorsException : DistributedTaskException
    {
        public ExtensionIsPublicAndHasPipelineDecoratorsException(String message)
            : base(message)
        {
        }

        public ExtensionIsPublicAndHasPipelineDecoratorsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private ExtensionIsPublicAndHasPipelineDecoratorsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskGroupUpdateFailedException : DistributedTaskException
    {
        public TaskGroupUpdateFailedException(String message)
            : base(message)
        {
        }

        public TaskGroupUpdateFailedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskGroupUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskGroupCyclicDependencyException : DistributedTaskException
    {
        public TaskGroupCyclicDependencyException(String message)
            : base(message)
        {
        }

        public TaskGroupCyclicDependencyException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskGroupCyclicDependencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "TaskIdsDoNotMatch", "GitHub.DistributedTask.WebApi.TaskIdsDoNotMatch, GitHub.DistributedTask.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class TaskIdsDoNotMatchException : DistributedTaskException
    {
        public TaskIdsDoNotMatchException(String message)
            : base(message)
        {
        }

        public TaskIdsDoNotMatchException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskIdsDoNotMatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class VariableGroupExistsException : DistributedTaskException
    {
        public VariableGroupExistsException(String message)
            : base(message)
        {
        }

        public VariableGroupExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private VariableGroupExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class VariableGroupNotFoundException : DistributedTaskException
    {
        public VariableGroupNotFoundException(String message)
            : base(message)
        {
        }

        public VariableGroupNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private VariableGroupNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class OAuthConfigurationExistsException : DistributedTaskException
    {
        public OAuthConfigurationExistsException(String message)
            : base(message)
        {
        }

        public OAuthConfigurationExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private OAuthConfigurationExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class OAuthConfigurationNotFoundException : DistributedTaskException
    {
        public OAuthConfigurationNotFoundException(String message)
            : base(message)
        {
        }

        public OAuthConfigurationNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private OAuthConfigurationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class RegularExpressionInvalidOptionsException : DistributedTaskException
    {
        public RegularExpressionInvalidOptionsException(String message)
            : base(message)
        {
        }

        public RegularExpressionInvalidOptionsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected RegularExpressionInvalidOptionsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class RegularExpressionValidationFailureException : DistributedTaskException
    {
        public RegularExpressionValidationFailureException(String message)
            : base(message)
        {
        }

        public RegularExpressionValidationFailureException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected RegularExpressionValidationFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class SecureFileExistsException : DistributedTaskException
    {
        public SecureFileExistsException(String message)
            : base(message)
        {
        }

        public SecureFileExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SecureFileExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class SecureFileNotFoundException : DistributedTaskException
    {
        public SecureFileNotFoundException(String message)
            : base(message)
        {
        }

        public SecureFileNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SecureFileNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class TaskOrchestrationPlanLogNotFoundException : DistributedTaskException
    {
        public TaskOrchestrationPlanLogNotFoundException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationPlanLogNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationPlanLogNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    [Serializable]
    public sealed class TaskOrchestrationPlanGroupNotFoundException : DistributedTaskException
    {
        public TaskOrchestrationPlanGroupNotFoundException(String message)
            : base(message)
        {
        }

        public TaskOrchestrationPlanGroupNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TaskOrchestrationPlanGroupNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class XPathJTokenParseException : DistributedTaskException
    {
        public XPathJTokenParseException(string message)
            : base(message)
        {
        }

        public XPathJTokenParseException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected XPathJTokenParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


    [Serializable]
    public class InvalidDatasourceException : DistributedTaskException
    {
        public InvalidDatasourceException(string message)
            : base(message)
        {
        }

        public InvalidDatasourceException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidDatasourceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


    [Serializable]
    public class CannotDeleteAndAddMetadataException : DistributedTaskException
    {
        public CannotDeleteAndAddMetadataException(string message)
            : base(message)
        {
        }

        public CannotDeleteAndAddMetadataException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected CannotDeleteAndAddMetadataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidTaskAgentCloudException : DistributedTaskException
    {
        public InvalidTaskAgentCloudException(string message) : base(message)
        {
        }

        public InvalidTaskAgentCloudException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidTaskAgentCloudException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentCloudExistsException : DistributedTaskException
    {
        public TaskAgentCloudExistsException(string message)
            : base(message)
        {
        }

        public TaskAgentCloudExistsException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskAgentCloudExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentCloudNotFoundException : DistributedTaskException
    {
        public TaskAgentCloudNotFoundException(string message)
            : base(message)
        {
        }

        public TaskAgentCloudNotFoundException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskAgentCloudNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentCloudRequestExistsException : DistributedTaskException
    {
        public TaskAgentCloudRequestExistsException(string message)
            : base(message)
        {
        }

        public TaskAgentCloudRequestExistsException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskAgentCloudRequestExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentCloudRequestNotFoundException : DistributedTaskException
    {
        public TaskAgentCloudRequestNotFoundException(string message)
            : base(message)
        {
        }

        public TaskAgentCloudRequestNotFoundException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskAgentCloudRequestNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentCloudRequestAlreadyCompleteException : DistributedTaskException
    {
        public TaskAgentCloudRequestAlreadyCompleteException(string message)
            : base(message)
        {
        }

        public TaskAgentCloudRequestAlreadyCompleteException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskAgentCloudRequestAlreadyCompleteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentPoolReferencesDifferentAgentCloudException : DistributedTaskException
    {
        public TaskAgentPoolReferencesDifferentAgentCloudException(string message)
            : base(message)
        {
        }

        public TaskAgentPoolReferencesDifferentAgentCloudException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskAgentPoolReferencesDifferentAgentCloudException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class PrivateTaskAgentProvisioningStateInvalidException : DistributedTaskException
    {
        public PrivateTaskAgentProvisioningStateInvalidException(string message)
            : base(message)
        {
        }

        public PrivateTaskAgentProvisioningStateInvalidException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected PrivateTaskAgentProvisioningStateInvalidException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class AgentFileNotFoundException : DistributedTaskException
    {
        public AgentFileNotFoundException(string message)
            : base(message)
        {
        }

        public AgentFileNotFoundException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected AgentFileNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class AgentMediaTypeNotSupportedException : DistributedTaskException
    {
        public AgentMediaTypeNotSupportedException(string message)
            : base(message)
        {
        }

        public AgentMediaTypeNotSupportedException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected AgentMediaTypeNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class TaskAgentCloudCommunicationException : DistributedTaskException
    {
        public TaskAgentCloudCommunicationException(string message)
            : base(message)
        {
        }

        public TaskAgentCloudCommunicationException(string message, Exception ex)
            : base(message, ex)
        {
        }

        protected TaskAgentCloudCommunicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class EnvironmentExistsException : DistributedTaskException
    {
        public EnvironmentExistsException(String message)
            : base(message)
        {
        }

        public EnvironmentExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EnvironmentExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class EnvironmentNotFoundException : DistributedTaskException
    {
        public EnvironmentNotFoundException(String message)
            : base(message)
        {
        }

        public EnvironmentNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EnvironmentNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class EnvironmentResourceExistsException : DistributedTaskException
    {
        public EnvironmentResourceExistsException(String message)
            : base(message)
        {
        }

        public EnvironmentResourceExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EnvironmentResourceExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class EnvironmentResourceNotFoundException : DistributedTaskException
    {
        public EnvironmentResourceNotFoundException(String message)
            : base(message)
        {
        }

        public EnvironmentResourceNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EnvironmentResourceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class EnvironmentResourcesExceededMaxCountException : DistributedTaskException
    {
        public EnvironmentResourcesExceededMaxCountException(String message)
            : base(message)
        {
        }

        public EnvironmentResourcesExceededMaxCountException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EnvironmentResourcesExceededMaxCountException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class EnvironmentExecutionDeploymentHistoryRecordNotFoundException : DistributedTaskException
    {
        public EnvironmentExecutionDeploymentHistoryRecordNotFoundException(String message)
            : base(message)
        {
        }

        public EnvironmentExecutionDeploymentHistoryRecordNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EnvironmentExecutionDeploymentHistoryRecordNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class EnvironmentPoolAlreadyInUseException : DistributedTaskException
    {
        public EnvironmentPoolAlreadyInUseException(String message)
            : base(message)
        {
        }

        public EnvironmentPoolAlreadyInUseException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private EnvironmentPoolAlreadyInUseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class InvalidContinuationTokenException : DistributedTaskException
    {
        public InvalidContinuationTokenException(String message)
            : base(message)
        {
        }

        public InvalidContinuationTokenException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        private InvalidContinuationTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
