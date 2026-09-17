using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;

namespace SmartParking.Api.Security
{
    public sealed class JwtAuthenticationFilter : IAuthenticationFilter
    {
        public bool AllowMultiple => false;

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            AuthenticationHeaderValue authorization = context.Request.Headers.Authorization;
            if (authorization == null || !string.Equals(authorization.Scheme, "Bearer", System.StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(authorization.Parameter))
            {
                context.ErrorResult = new AuthFailureResult(context.Request, "Authentication required.");
                return Task.CompletedTask;
            }

            try
            {
                context.Principal = JwtTokenService.Validate(authorization.Parameter);
            }
            catch
            {
                context.ErrorResult = new AuthFailureResult(context.Request, "Invalid or expired session. Please log in again.");
            }

            return Task.CompletedTask;
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private sealed class AuthFailureResult : IHttpActionResult
        {
            private readonly HttpRequestMessage _request;
            private readonly string _message;

            public AuthFailureResult(HttpRequestMessage request, string message)
            {
                _request = request;
                _message = message;
            }

            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = _request.CreateResponse(HttpStatusCode.Unauthorized, new { error = _message });
                return Task.FromResult(response);
            }
        }
    }
}
