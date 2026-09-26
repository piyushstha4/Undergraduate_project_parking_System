using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace SmartParking.Api.Security
{
    public sealed class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            var authenticated = actionContext.RequestContext.Principal?.Identity?.IsAuthenticated == true;
            var status = authenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;
            var message = authenticated
                ? "You do not have permission to perform this action."
                : "Authentication required.";
            actionContext.Response = actionContext.Request.CreateResponse(status, new { error = message });
        }
    }
}
