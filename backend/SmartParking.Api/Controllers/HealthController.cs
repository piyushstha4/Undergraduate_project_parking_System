using System.Web.Http;

namespace SmartParking.Api.Controllers
{
    [RoutePrefix("api")]
    public sealed class HealthController : ApiController
    {
        [HttpGet, Route("health"), AllowAnonymous]
        public IHttpActionResult Get()
        {
            return Ok(new { status = "ok", service = "smart-parking-api" });
        }
    }
}
