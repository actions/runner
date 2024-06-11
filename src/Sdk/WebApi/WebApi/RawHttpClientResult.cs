using System.Net;

namespace Sdk.WebApi.WebApi
{
    public class RawHttpClientResult
    {
        public bool IsSuccess { get; protected set; }

        /// <summary>
        /// A description of the HTTP status code, like "Error: Unprocessable Entity"
        /// </summary>
        public string Error { get; protected set; }

        /// <summary>
        /// The raw of the HTTP response, for unsuccessful HTTP status codes
        /// </summary>
        public string ErrorContent { get; protected set; }

        public HttpStatusCode StatusCode { get; protected set; }

        public bool IsFailure => !IsSuccess;

        protected RawHttpClientResult(bool isSuccess, string error, HttpStatusCode statusCode, string errorContent = null)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
            ErrorContent = errorContent;
        }
    }

    public class RawHttpClientResult<T> : RawHttpClientResult
    {
        public T Value { get; private set; }

        protected internal RawHttpClientResult(T value, bool isSuccess, string error, HttpStatusCode statusCode, string errorContent)
            : base(isSuccess, error, statusCode, errorContent)
        {
            Value = value;
        }

        public static RawHttpClientResult<T> Fail(string message, HttpStatusCode statusCode, string errorContent) => new RawHttpClientResult<T>(default(T), false, message, statusCode, errorContent);
        public static RawHttpClientResult<T> Ok(T value) => new RawHttpClientResult<T>(value, true, string.Empty, HttpStatusCode.OK, null);
    }
}
