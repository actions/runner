using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class CredentialData
    {
        public string Scheme { get; set; }

        public Dictionary<string, string> Data
        {
            get
            {
                if (_data == null)
                {
                    _data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                return _data;
            }
        }

        private Dictionary<string, string> _data;
    }
}