using System;

namespace GitHub.Services.WebApi.Internal
{
    /// <summary>
    /// GenClient (SwaggerGenerator) will ignore controller methods, parameters, and classes that have this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
    public sealed class ClientIgnoreAttribute : Attribute
    {
        public ClientIgnoreAttribute()
        {
        }
    }

    [Flags]
    public enum RestClientLanguages
    {
        All = ~0,
        CSharp = 1,
        Java = 2,
        TypeScript = 4,
        Nodejs = 8,
        [Obsolete("DocMD has been replaced by Swagger generated REST Documentation.")]
        DocMD = 16,
        Swagger2 = 32,
        Python = 64,
        TypeScriptWebPlatform = 128
    }
}
