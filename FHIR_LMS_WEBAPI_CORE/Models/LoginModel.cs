using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class LoginModel
    {
        private readonly IConfiguration _configuration;
        private readonly string fhirUrl;
        HTTPrequest HTTPrequest;
        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
            HTTPrequest = new HTTPrequest();
        }

        public JObject Check(JObject personUser, JObject loginData, string token)
        {
            dynamic result = new JObject();
            if (personUser["resourceType"].ToString() == "Bundle" && personUser["total"].ToString() == "1")
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

                            result.Roles = GetRoles(personUser, loginData);

                            result.Message = "Login success.";
                            return result;
                        }
                    }
                }
            }
            result.Message = "Incorrect email or password.";
            return result;
        }

        private JArray GetRoles(JObject personUser, JObject loginData)
        {
            dynamic result = new JObject();

            //Get Roles
            JArray roles = new JArray();

            foreach (JObject role in (JArray)personUser["link"])
            {
                //Get Patient Roles
                if (role["target"]["reference"].ToString().StartsWith("Patient/"))
                {
                    roles.Add(role["target"]["reference"].ToString());
                }
                //Get Practitioner Roles
                else if (role["target"]["reference"].ToString().StartsWith("Practitioner/"))
                {
                    string practitionerID = role["target"]["reference"].ToString().Substring(13);

                    HTTPrequest HTTPrequest = new HTTPrequest();
                    string param = "?practitioner=" + practitionerID;
                    JObject res = HTTPrequest.getResource(fhirUrl, "PractitionerRole", param, null, null, loginData);
                    int total = res["total"].Value<int>();

                    JArray pracRoles = (JArray)res["entry"];

                    for (int i = 0; i < total; i++)
                    {
                        roles.Add(String.Concat("PractitionerRole/", pracRoles[i]["resource"]["id"].ToString()));
                    }
                }
            }

            return roles;
        }

    }
}
