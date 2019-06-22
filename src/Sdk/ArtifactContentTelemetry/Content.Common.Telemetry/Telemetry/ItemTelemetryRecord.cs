using System;
using GitHub.Services.Common;

namespace GitHub.Services.Content.Common.Telemetry
{
    public class ItemTelemetryRecord : ActionTelemetryRecord
    {
        private string itemName;

        public string ItemName
        {
            get { return OmitPerCompliance(this.itemName); }
        }

        public ItemTelemetryRecord(TelemetryInformationLevel level, Uri baseAddress, string actionName, string itemName, uint attemptNumber = 1) 
            : base(level, baseAddress, actionName, attemptNumber)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(itemName, nameof(itemName));
            this.itemName = itemName;
        }

        public ItemTelemetryRecord(ItemTelemetryRecord record) : base(record)
        {
            this.itemName = record.ItemName;
        }

        public override ErrorTelemetryRecord CaptureError(Exception exception)
        {
            base.CaptureError(exception);
            return new ErrorTelemetryRecord(this);
        }
    }
}
