using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace SmartParking.Api.Infrastructure
{
    public sealed class ApiExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is ApiException api)
            {
                context.Response = context.Request.CreateResponse(
                    (HttpStatusCode)api.StatusCode,
                    new { error = api.Message });
                return;
            }

            System.Diagnostics.Trace.TraceError(context.Exception.ToString());
            context.Response = context.Request.CreateResponse(
                HttpStatusCode.InternalServerError,
                new { error = "Something went wrong on the server. Please try again." });
        }
    }
}
