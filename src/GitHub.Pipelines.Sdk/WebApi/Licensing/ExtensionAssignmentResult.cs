using Microsoft.VisualStudio.Services.Common;
using System;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public class ExtensionOperationResult
    {
        public ExtensionOperationResult(Guid accountId, Guid userId, string extensionId, ExtensionOperation operation)
        {
            ArgumentUtility.CheckForEmptyGuid(accountId, "accountId");
            ArgumentUtility.CheckForEmptyGuid(userId, "userId");
            ArgumentUtility.CheckStringForNullOrEmpty(extensionId, "extensionId");

            this.AccountId = accountId;
            this.UserId = userId;
            this.ExtensionId = extensionId;
            this.Operation = operation;
        }

        public Guid AccountId { get; }

        public Guid UserId { get; set; }

        public string ExtensionId { get; }

        public ExtensionOperation Operation { get; }

        public OperationResult Result { get; set; }

        public string Message { get; set; }

        public override string ToString()
        {
            return $"{this.AccountId}|{this.UserId}|{this.ExtensionId}|{this.Message}|{this.Result.ToString()}";
        }
    }
}
