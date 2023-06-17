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

                            result.roles = GetRoles(personUser, loginData);

                            result.code = 200;
                            return result;
                        }
                    }
                }
            }
            result.code = 401;
            return result;
        }

        private JArray GetRoles(JObject personUser, JObject loginData)
        {
            //Get Roles
            JArray roles = new JArray();

            foreach (JObject role in (JArray)personUser["link"])
            {
                string roleID = role["target"]["reference"].ToString().Split('/')[1];
                JObject userRole = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Assets/JSON/Role.json")));
                loginData["errmsg"] = "Error fetching user roles.";

                //Get Patient Roles
                if (role["target"]["reference"].ToString().StartsWith("Patient/"))
                {
                    JObject res = HTTPrequest.getResource(fhirUrl, "Patient", "/" + roleID, "", null, loginData);

                    userRole["roleName"] = res["resourceType"];
                    userRole["roleID"] = res["id"];
                    userRole["practID"] = "";
                    userRole["organizationID"] = res["managingOrganization"]["reference"].ToString().Split('/')[1];
                    userRole["organizationName"] = res["managingOrganization"]["display"] ?? res["managingOrganization"]["reference"];
                    userRole["status"] = (bool)res["active"] ? "active":"";

                    roles.Add(userRole);
                }
                //Get Practitioner Roles
                else if (role["target"]["reference"].ToString().StartsWith("Practitioner/"))
                {
                    string practitionerID = role["target"]["reference"].ToString().Substring(13);

                    string param = "?practitioner=" + practitionerID;
                    JObject res = HTTPrequest.getResource(fhirUrl, "PractitionerRole", param, null, null, loginData);

                    JArray pracRoles = (JArray)res["entry"];

                    foreach (JObject pracRole in pracRoles)
                    {
                        userRole["roleName"] = pracRole["resource"]["resourceType"];
                        userRole["roleID"] = pracRole["resource"]["id"];
                        userRole["practID"] = pracRole["resource"]["practitioner"]["reference"].ToString().Split('/')[1];
                        userRole["organizationID"] = pracRole["resource"]["organization"]["reference"].ToString().Split('/')[1];
                        userRole["organizationName"] = pracRole["resource"]["organization"]["display"];
                        userRole["status"] = (bool)pracRole["resource"]["active"] ? "active" : "";

                        foreach (JObject coding in pracRole["resource"]["code"][0]["coding"])
                        {
                            userRole["roleCode"].ToList().Add(coding["code"]);
                        }

                        roles.Add(userRole);
                    }
                }
            }

            return roles;
        }

    }
}
