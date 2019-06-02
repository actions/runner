using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Zeus
{
    public enum DatabaseMigrationType
    {
        Import = 0,
        Export = 1
    }

    public enum DatabaseMigrationStatus
    {
        Created = 0,
        Running = 1,
        Failed = 2,
        Completed = 3
    }

    [DataContract]
    public class DatabaseMigration
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int MigrationId { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String SqlInstance { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String UserId { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Password { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String StorageAccount { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Container { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Databases { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DatabaseMigrationType MigrationType { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int DatabasesMigrated { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DatabaseMigrationStatus Status { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? QueuedTime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string StatusMessage { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid JobId { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String DatabasePrefix{ get; set; }

        public override string ToString()
        {
            // Intentionally not writing connection string.
            return String.Format(
                CultureInfo.InvariantCulture,
                @"DatabaseMigration
[
    MigrationId:           {0}
    JobId:                 {1}
    SqlInstance:           {2}
    UserId:                {3}
    DatabasePrefix         {4}
    Container:             {5}
    Databases:             {6}
    MigrationType:         {7}
    DatabasesMigrated:     {8}
    QueuedTime:            {9}
    StartTime:             {10}
    EndTime:               {11}
    Status:                {12}
    StatusMessage:         {13}
]",
                MigrationId,
                JobId,
                SqlInstance,
                UserId,
                DatabasePrefix,
                Container,
                Databases,
                MigrationType,
                DatabasesMigrated,
                QueuedTime,
                StartTime,
                EndTime,
                Status,
                StatusMessage
                );
        }
    }
}
