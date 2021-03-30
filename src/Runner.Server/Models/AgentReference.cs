namespace Runner.Server.Models
{
    public class AgentReference
    {
        public int Id { get; set; }
        // public string[] Labels { get; set; }
        public byte[] Exponent { get; set; }
        public byte[] Modulus { get; set; }
    }
}