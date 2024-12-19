using System.Runtime.Serialization;

namespace Sdk.RSWebApi.Contracts
{
    [DataContract]
    public struct Annotation
    {
        [DataMember(Name = "level", EmitDefaultValue = false)]
        public AnnotationLevel Level;

        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message;

        [DataMember(Name = "rawDetails", EmitDefaultValue = false)]
        public string RawDetails;

        [DataMember(Name = "path", EmitDefaultValue = false)]
        public string Path;

        [DataMember(Name = "isInfrastructureIssue", EmitDefaultValue = false)]
        public bool IsInfrastructureIssue;

        [DataMember(Name = "startLine", EmitDefaultValue = false)]
        public long StartLine;

        [DataMember(Name = "endLine", EmitDefaultValue = false)]
        public long EndLine;

        [DataMember(Name = "startColumn", EmitDefaultValue = false)]
        public long StartColumn;

        [DataMember(Name = "endColumn", EmitDefaultValue = false)]
        public long EndColumn;

        [DataMember(Name = "stepNumber", EmitDefaultValue = false)]
        public long StepNumber;
    }
}
