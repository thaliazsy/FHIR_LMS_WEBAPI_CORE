
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class JWTModel
    {
        private readonly IConfiguration _configuration;
        private readonly string fhirUrl;
        private readonly string _secret;
        private readonly string _portal;
        HTTPrequest HTTPrequest;

        public JWTModel(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
            _secret = _configuration.GetValue<string>("secret_key");
            _portal = _configuration.GetValue<string>("Portal_Url");
            HTTPrequest = new HTTPrequest();
        }

        /// <summary>  
        /// Generate JWT access token. Scope contains one document (FHIR Document)
        /// </summary>
        /// <returns>
        /// JWT access token
        /// </returns>
        public string GenerateAccessToken(string _aud, string _sub, string endpoint)
        {
            JArray _scope = new JArray();
            JObject s = new JObject();
            s["url"] = endpoint;
            _scope.Add(s);

            string scope = JsonConvert.SerializeObject(_scope);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var token = new JwtSecurityToken(
                claims: new Claim[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti,"jwt-token-id" ),
                    new Claim(JwtRegisteredClaimNames.Iss, _portal),
                    new Claim(JwtRegisteredClaimNames.Aud, _aud),
                    new Claim(JwtRegisteredClaimNames.Sub, _sub),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new Claim("scope", scope),
                },
                notBefore: new DateTimeOffset(DateTime.Now).DateTime,
                expires: new DateTimeOffset(DateTime.Now.AddMinutes(10)).DateTime,
                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);

            //JwtBuilder jwtBuilder = new JwtBuilder()
            //    .WithAlgorithm(new HMACSHA256Algorithm())
            //    .WithSecret(_secret)
            //    .AddClaim("jti", "jwt-token-id")  // JWT ID
            //    .AddClaim("iss", _portal)  // issued by: portal domain url
            //    .AddClaim("aud", _aud)  // audience: resource repository url 
            //    .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())  // issued at (time)
            //    .AddClaim("nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds())  // not valid before (time)
            //    .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds())  // exp in 10mins after generated.
            //    .AddClaim("sub", _sub)  // subject ID
            //    .AddClaim("scope", scope);  //JsonConvert.SerializeObject(_scope)
            //                                // subject ID
            //return jwtBuilder.Encode();
        }

        /// <summary>  
        /// Generate JWT access token. Scope contains multiple documents (FHIR Document & others)  
        /// </summary>
        /// <returns>
        /// JWT access token
        /// </returns>
        public string GenerateAccessToken(string _aud, string _sub, string[] endpoints)
        {
            JArray _scope = new JArray();
            foreach (string endpoint in endpoints)
            {
                JObject s = new JObject();
                s["url"] = endpoint;
                _scope.Add(s);
            }

            string scope = JsonConvert.SerializeObject(_scope);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var token = new JwtSecurityToken(
                claims: new Claim[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti,"jwt-token-id" ),
                    new Claim(JwtRegisteredClaimNames.Iss, _portal),
                    new Claim(JwtRegisteredClaimNames.Aud, _aud),
                    new Claim(JwtRegisteredClaimNames.Sub, _sub),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new Claim("scope", scope),
                },
                notBefore: new DateTimeOffset(DateTime.Now).DateTime,
                expires: new DateTimeOffset(DateTime.Now.AddMinutes(10)).DateTime,
                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
