using System.Web.Http;
using Microsoft.Owin.Cors;
using Newtonsoft.Json;
using Owin;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Security;

namespace SmartParking.Api
{
    public sealed class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.Filters.Add(new ApiExceptionFilter());
            config.Filters.Add(new JwtAuthenticationFilter());

            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = JsonConfig.Create().ContractResolver;
            json.SerializerSettings.NullValueHandling = NullValueHandling.Include;
            json.SerializerSettings.DateParseHandling = DateParseHandling.None;
            json.SerializerSettings.Formatting = Formatting.None;
            config.Formatters.Clear();
            config.Formatters.Add(json);

            app.UseCors(CorsOptions.AllowAll);
            app.UseWebApi(config);
        }
    }
}
