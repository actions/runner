using System;
using System.Collections.Generic;

namespace GitHub.Runner.Common
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
