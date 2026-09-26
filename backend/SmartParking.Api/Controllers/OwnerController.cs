using System.Web.Http;
using SmartParking.Api.Security;

namespace SmartParking.Api.Controllers
{
    [RoutePrefix("api/owner")]
    [AuthorizeRoles("parking_owner", "admin")]
    public sealed class OwnerController : ApiControllerBase
    {
        [HttpGet, Route("reports")]
        public IHttpActionResult Reports()
        {
            return Ok(AppServices.Owner.Reports(CurrentUserId, CurrentRole));
        }
    }
}
