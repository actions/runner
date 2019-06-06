using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace GitHub.Services.Account
{

    [DataContract]
    public sealed class AccountNameAvailability
    {
        private AccountNameAvailability()
        {
        }

        public AccountNameAvailability(bool isValidName, string statusReason) : this()
        {
            IsValidName= isValidName;
            StatusReason = statusReason ?? string.Empty;
        }

        [DataMember]
        public bool IsValidName { get; }

        /// <summary>
        /// Reason for current status
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String StatusReason { get; }

    }
}
