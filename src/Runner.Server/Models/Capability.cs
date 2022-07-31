namespace Runner.Server.Models {
    public class Capability {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public bool System { get; set; }
    }
}