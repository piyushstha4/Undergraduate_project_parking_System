using System.Web.Http;
using SmartParking.Api.Models;
using SmartParking.Api.Security;

namespace SmartParking.Api.Controllers
{
    [RoutePrefix("api/admin")]
    [AuthorizeRoles("admin")]
    public sealed class AdminController : ApiControllerBase
    {
        [HttpGet, Route("stats")]
        public IHttpActionResult Stats()
        {
            return Ok(AppServices.Admin.Stats());
        }

        [HttpGet, Route("users")]
        public IHttpActionResult Users(string role = null)
        {
            return Ok(new { users = AppServices.Admin.Users(role) });
        }

        [HttpPut, Route("users/{id:int}/status")]
        public IHttpActionResult UpdateStatus(int id, StatusUpdateRequest request)
        {
            return Ok(new { user = AppServices.Admin.UpdateStatus(id, request?.Status) });
        }

        [HttpGet, Route("parking-areas")]
        public IHttpActionResult ParkingAreas()
        {
            return Ok(new { areas = AppServices.Admin.ParkingAreas() });
        }
    }
}
