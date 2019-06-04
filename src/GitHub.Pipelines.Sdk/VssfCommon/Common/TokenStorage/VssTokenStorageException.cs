using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Common.TokenStorage
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "VssTokenStorageException", "GitHub.Services.Common.TokenStorage.VssTokenStorageException, GitHub.Services.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class VssTokenStorageException : Exception
    {
        public VssTokenStorageException(String message)
            : base(message)
        {
        }

       public  VssTokenStorageException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected VssTokenStorageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
