using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SmartParking.Api.Infrastructure
{
    public static class JsonConfig
    {
        public static JsonSerializerSettings Create()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = true,
                        OverrideSpecifiedNames = false
                    }
                },
                NullValueHandling = NullValueHandling.Include,
                DateParseHandling = DateParseHandling.None
            };
        }
    }
}
