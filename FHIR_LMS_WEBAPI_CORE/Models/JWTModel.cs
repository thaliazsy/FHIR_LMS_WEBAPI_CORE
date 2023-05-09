using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class JWTModel
    {
        private readonly IConfiguration _configuration;
        private static readonly string _secret = "H707tcumi110";
        private static readonly string _portal = "http://203.64.84.33:33484/";
        private readonly string fhirUrl;
        HTTPrequest HTTPrequest;

        public JWTModel(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
            HTTPrequest = new HTTPrequest();
        }

        public static string GenerateAccessToken(string _aud, string _sub, string docUrl)
        {
            JArray _scope = new JArray();
            JObject s = new JObject();
            s["url"] = docUrl;
            _scope.Add(s);

            string scope = JsonConvert.SerializeObject(_scope);

            JwtBuilder jwtBuilder = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Encoding.ASCII.GetBytes(_secret))
                .AddClaim("jti", "jwt-token-id")  // JWT ID
                .AddClaim("iss", _portal)  // issued by: portal domain url
                .AddClaim("aud", _aud)  // audience: resource repository url 
                .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())  // issued at (time)
                .AddClaim("nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds())  // not valid before (time)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds())  // exp in 10mins after generated.
                .AddClaim("sub", _sub)  // subject ID
                .AddClaim("scope", scope);  //JsonConvert.SerializeObject(_scope)
                                            // subject ID
            return jwtBuilder.Encode();
        }
        
        public static string GenerateAccessToken(string _aud, string _sub, string[] endpoints)
        {
            JArray _scope = new JArray();
            foreach (string endpoint in endpoints)
            {
                JObject s = new JObject();
                s["url"] = endpoint;
                _scope.Add(s);
            }

            string scope = JsonConvert.SerializeObject(_scope);

            JwtBuilder jwtBuilder = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Encoding.ASCII.GetBytes(_secret))
                .AddClaim("jti", "jwt-token-id")  // JWT ID
                .AddClaim("iss", _portal)  // issued by: portal domain url
                .AddClaim("aud", _aud)  // audience: resource repository url 
                .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())  // issued at (time)
                .AddClaim("nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds())  // not valid before (time)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds())  // exp in 10mins after generated.
                .AddClaim("sub", _sub)  // subject ID
                .AddClaim("scope", scope);  //JsonConvert.SerializeObject(_scope)
                                            // subject ID
            return jwtBuilder.Encode();
        }
    }
}
