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
        /// The HTTP response body for unsuccessful HTTP status codes, or an error message when reading the response body fails.
        /// </summary>
        public string ErrorBody { get; protected set; }

        public HttpStatusCode StatusCode { get; protected set; }

        public bool IsFailure => !IsSuccess;

        public RawHttpClientResult(bool isSuccess, string error, HttpStatusCode statusCode, string errorBody = null)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
            ErrorBody = errorBody;
        }
    }

    public class RawHttpClientResult<T> : RawHttpClientResult
    {
        public T Value { get; private set; }

        protected internal RawHttpClientResult(T value, bool isSuccess, string error, HttpStatusCode statusCode, string errorBody)
            : base(isSuccess, error, statusCode, errorBody)
        {
            Value = value;
        }

        public static RawHttpClientResult<T> Fail(string message, HttpStatusCode statusCode, string errorBody) => new RawHttpClientResult<T>(default(T), false, message, statusCode, errorBody);
        public static RawHttpClientResult<T> Ok(T value) => new RawHttpClientResult<T>(value, true, string.Empty, HttpStatusCode.OK, null);
    }
}
