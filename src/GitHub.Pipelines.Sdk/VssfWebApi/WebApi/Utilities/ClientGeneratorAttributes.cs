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

    /// <summary>
    /// When a method or class has this attribute, we will only generate client methods for the specified languages.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class ClientIncludeAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="languages">A list of languages to generate methods for.</param>
        public ClientIncludeAttribute(RestClientLanguages languages)
        {
            Languages = languages;
        }

        public RestClientLanguages Languages { get; private set; }
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

    /// <summary>
    /// Suppresses the default constant enum generation behavior in typescriptwebplatform clientgen. When using this attribute, and affected code generation will product a .ts file instead of a .d.ts file (non-constant enumerations should not be generated into .d.ts files).
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class ClientDontGenerateTypeScriptEnumAsConst : Attribute
    {
        public ClientDontGenerateTypeScriptEnumAsConst()
        {
        }
    }
}
