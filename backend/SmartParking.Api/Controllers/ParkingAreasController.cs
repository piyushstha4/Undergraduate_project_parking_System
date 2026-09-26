using System.Net;
using System.Web.Http;
using SmartParking.Api.Models;
using SmartParking.Api.Security;

namespace SmartParking.Api.Controllers
{
    [RoutePrefix("api/parking-areas")]
    public sealed class ParkingAreasController : ApiControllerBase
    {
        [HttpGet, Route(""), AllowAnonymous]
        public IHttpActionResult Search(string q = null, string city = null, string maxPrice = null, string vehicleType = null, string onlyAvailable = null, string sort = null)
        {
            return Ok(new { areas = AppServices.Areas.Search(q, city, maxPrice, vehicleType, onlyAvailable, sort) });
        }

        [HttpGet, Route("owner/mine"), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult Mine()
        {
            return Ok(new { areas = AppServices.Areas.Mine(CurrentUserId, IsAdmin) });
        }

        [HttpGet, Route("{id:int}"), AllowAnonymous]
        public IHttpActionResult Get(int id)
        {
            return Ok(AppServices.Areas.GetById(id));
        }

        [HttpPost, Route(""), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult Create(CreateParkingAreaRequest request)
        {
            var area = AppServices.Areas.Create(CurrentUserId, request);
            return Content(HttpStatusCode.Created, new { area });
        }

        [HttpPut, Route("{id:int}"), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult Update(int id, CreateParkingAreaRequest request)
        {
            return Ok(new { area = AppServices.Areas.Update(id, CurrentUserId, CurrentRole, request) });
        }

        [HttpDelete, Route("{id:int}"), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult Delete(int id)
        {
            AppServices.Areas.Delete(id, CurrentUserId, CurrentRole);
            return Ok(new { success = true });
        }

        [HttpPost, Route("{areaId:int}/slots"), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult CreateSlot(int areaId, CreateSlotRequest request)
        {
            var slot = AppServices.Slots.Create(areaId, CurrentUserId, CurrentRole, request);
            return Content(HttpStatusCode.Created, new { slot });
        }

        [HttpPost, Route("{areaId:int}/reviews"), AuthorizeRoles("user", "admin")]
        public IHttpActionResult CreateReview(int areaId, CreateReviewRequest request)
        {
            AppServices.Reviews.Create(areaId, CurrentUserId, request);
            return Content(HttpStatusCode.Created, new { success = true });
        }
    }
}
