using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class RegisterModel
    {
        private readonly IConfiguration _configuration;
        private readonly string fhirUrl;
        HTTPrequest HTTPrequest;
        public RegisterModel(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
            HTTPrequest = new HTTPrequest();
        }

        public JObject Check(JObject personUser, JObject loginData, string token)
        {
            dynamic result = new JObject();
            if (personUser["resourceType"].ToString() == "Bundle" && personUser["total"].ToString() == "0")
            {
                personUser = (JObject)personUser["entry"][0]["resource"];



            }

            //Verify email and password
            if (personUser["identifier"].HasValues && personUser["identifier"][0] != null && personUser["identifier"][1] != null)
            {
                if (personUser["identifier"][0]["system"].ToString() == "UserID" && personUser["identifier"][1]["system"].ToString() == "Password")
                {
                    string a = (string)personUser["identifier"][0]["value"];
                    string b = (string)loginData["person"]["email"];
                    if (a == b)
                    {
                        a = (string)personUser["identifier"][1]["value"];
                        b = (string)loginData["person"]["password"];
                        if (a == b)
                        {
                            //Email and password verified
                            loginData["person"]["id"] = personUser["id"] != null ? personUser["id"] : "";
                            loginData["person"]["name"] = personUser["name"][0]["text"] != null ? personUser["name"][0]["text"] : "";
                            loginData["person"]["email"] = personUser["identifier"][0] != null ? personUser["identifier"][0]["value"] : "";

                            

                            result.code = 200;
                            return result;
                        }
                    }
                }
            }
            result.code = 401;
            return result;
        }

      
    }
}
