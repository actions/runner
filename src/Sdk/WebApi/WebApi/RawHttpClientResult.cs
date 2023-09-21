using System.Net;

namespace Sdk.WebApi.WebApi
{
    public class RawHttpClientResult
    {
        public bool IsSuccess { get; protected set; }
        public string Error { get; protected set; }
        public HttpStatusCode StatusCode { get; protected set; }
        public bool IsFailure => !IsSuccess;

        protected RawHttpClientResult(bool isSuccess, string error, HttpStatusCode statusCode)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }
    }

    public class RawHttpClientResult<T> : RawHttpClientResult
    {
        public T Value { get; private set; }

        protected internal RawHttpClientResult(T value, bool isSuccess, string error, HttpStatusCode statusCode)
            : base(isSuccess, error, statusCode)
        {
            Value = value;
        }

        public static RawHttpClientResult<T> Fail(string message, HttpStatusCode statusCode) => new RawHttpClientResult<T>(default(T), false, message, statusCode);
        public static RawHttpClientResult<T> Ok(T value) => new RawHttpClientResult<T>(value, true, string.Empty, HttpStatusCode.OK);
    }
}
