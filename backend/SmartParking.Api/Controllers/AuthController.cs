using System.Net;
using System.Web.Http;
using SmartParking.Api.Models;
using SmartParking.Api.Security;

namespace SmartParking.Api.Controllers
{
    [RoutePrefix("api/auth")]
    public sealed class AuthController : ApiControllerBase
    {
        [HttpPost, Route("register"), AllowAnonymous]
        public IHttpActionResult Register(RegisterRequest request)
        {
            var result = AppServices.Auth.Register(request);
            return Content(HttpStatusCode.Created, result);
        }

        [HttpPost, Route("login"), AllowAnonymous]
        public IHttpActionResult Login(LoginRequest request)
        {
            return Ok(AppServices.Auth.Login(request));
        }

        [HttpGet, Route("me"), AuthorizeRoles]
        public IHttpActionResult Me()
        {
            return Ok(new { user = AppServices.Auth.Me(CurrentUserId) });
        }
    }
}
