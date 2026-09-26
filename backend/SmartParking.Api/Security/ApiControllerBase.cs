using System.Security.Claims;
using System.Security.Principal;
using System.Web.Http;
using SmartParking.Api.Infrastructure;

namespace SmartParking.Api.Security
{
    public abstract class ApiControllerBase : ApiController
    {
        protected int CurrentUserId
        {
            get
            {
                var value = FindClaim("id") ?? FindClaim(ClaimTypes.NameIdentifier);
                if (!int.TryParse(value, out var id))
                {
                    throw ApiException.Unauthorized("Authentication required.");
                }
                return id;
            }
        }

        protected string CurrentRole => FindClaim("role") ?? FindClaim(ClaimTypes.Role);

        protected bool IsAdmin => CurrentRole == "admin";

        private string FindClaim(string type)
        {
            var principal = User as ClaimsPrincipal ?? User as IPrincipal as ClaimsPrincipal;
            return principal?.FindFirst(type)?.Value;
        }
    }
}
