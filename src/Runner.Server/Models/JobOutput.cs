using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Threading;
using GitHub.DistributedTask.WebApi;
using static Runner.Server.Controllers.MessageController;

namespace Runner.Server.Models {
    public class JobOutput {
        public long Id { get; set; }
        [IgnoreDataMember]
        public Job Job { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}