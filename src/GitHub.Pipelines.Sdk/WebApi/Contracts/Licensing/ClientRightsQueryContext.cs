namespace Microsoft.VisualStudio.Services.Licensing
{
    public class ClientRightsQueryContext
    {
        public string Canary { get; set; }

        public bool IncludeCertificate { get; set; }

        public string MachineId { get; set; }

        public string ProductEdition { get; set; }

        public string ProductFamily { get; set; }

        public string ProductVersion { get; set; }

        public string ReleaseType { get; set; }
    }
}
