using System;
using System.ComponentModel;

namespace GitHub.Actions.Pipelines.WebApi
{
    /// <summary>
    /// $expand options for GetArtifact and ListArtifacts.
    /// </summary>
    [TypeConverter(typeof(KnownFlagsEnumTypeConverter))]
    [Flags]
    public enum GetArtifactExpandOptions
    {
        None = 0,
        SignedContent = 1,
    }
}
