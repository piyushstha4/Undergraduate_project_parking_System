using System.Web.Http;
using SmartParking.Api.Models;
using SmartParking.Api.Security;

namespace SmartParking.Api.Controllers
{
    [RoutePrefix("api/slots")]
    public sealed class SlotsController : ApiControllerBase
    {
        [HttpPut, Route("{id:int}"), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult Update(int id, UpdateSlotRequest request)
        {
            return Ok(new { slot = AppServices.Slots.Update(id, CurrentUserId, CurrentRole, request) });
        }

        [HttpDelete, Route("{id:int}"), AuthorizeRoles("parking_owner", "admin")]
        public IHttpActionResult Delete(int id)
        {
            AppServices.Slots.Delete(id, CurrentUserId, CurrentRole);
            return Ok(new { success = true });
        }
    }
}
