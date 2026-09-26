using System;

namespace SmartParking.Api.Infrastructure
{
    public sealed class ApiException : Exception
    {
        public int StatusCode { get; }

        public ApiException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public static ApiException BadRequest(string message) => new ApiException(400, message);
        public static ApiException Unauthorized(string message) => new ApiException(401, message);
        public static ApiException Forbidden(string message) => new ApiException(403, message);
        public static ApiException NotFound(string message) => new ApiException(404, message);
        public static ApiException Conflict(string message) => new ApiException(409, message);
    }
}
