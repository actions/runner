using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Content.Common
{
    [Serializable]
    public class EnumeratorAlreadyStartedException : Exception
    {
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp

        public EnumeratorAlreadyStartedException()
        {
        }

        public EnumeratorAlreadyStartedException(string message) 
            : base(message)
        {
        }

        public EnumeratorAlreadyStartedException(string message, Exception inner) 
            : base(message, inner)
        {
        }

        protected EnumeratorAlreadyStartedException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
