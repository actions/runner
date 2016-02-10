using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceLocatorAttribute : Attribute
    {
        public Type Default { get; set; }

        public static readonly String DefaultPropertyName = "Default";
    }
}