using System.Net;
using System.Web.Http;
using SmartParking.Api.Models;
using SmartParking.Api.Security;

namespace SmartParking.Api.Controllers
{
    [RoutePrefix("api/bookings")]
    public sealed class BookingsController : ApiControllerBase
    {
        [HttpPost, Route(""), AuthorizeRoles("user", "admin")]
        public IHttpActionResult Create(CreateBookingRequest request)
        {
            var booking = AppServices.Bookings.Create(CurrentUserId, request);
            return Content(HttpStatusCode.Created, new { booking });
        }

        [HttpGet, Route("me"), AuthorizeRoles]
        public IHttpActionResult Mine()
        {
            return Ok(new { bookings = AppServices.Bookings.ForUser(CurrentUserId) });
        }

        [HttpGet, Route(""), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult List(string areaId = null, string status = null)
        {
            return Ok(new { bookings = AppServices.Bookings.ForOwnerOrAdmin(CurrentUserId, CurrentRole, areaId, status) });
        }

        [HttpPut, Route("{id:int}/cancel"), AuthorizeRoles]
        public IHttpActionResult Cancel(int id)
        {
            return Ok(new { booking = AppServices.Bookings.Cancel(id, CurrentUserId, CurrentRole) });
        }
    }
}
