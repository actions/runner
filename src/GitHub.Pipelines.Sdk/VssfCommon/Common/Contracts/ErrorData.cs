using System;

namespace GitHub.Services.Common.Contracts
{
    public class ErrorData
    {
        public Uri Uri { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string Content { get; set; }
        public string Identity { get; set; }
    }
}
