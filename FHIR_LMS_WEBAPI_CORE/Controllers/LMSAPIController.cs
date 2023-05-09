using FHIR_LMS_WEBAPI_CORE.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FHIR_LMS_WEBAPI_CORE.Controllers
{
    public class LMSAPIController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string fhirUrl;
        private JObject loginData;
        private HTTPrequest HTTPrequest = new HTTPrequest();
        public LMSAPIController(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
            loginData = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "JSON/LoginData.json")));
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        [HttpPost("api/SelectCourse")]
        public IActionResult SelectCourse([FromBody] UserLogin user)
        {
            SelectCourse selectCourse = new SelectCourse(_configuration);

            var headers = Request.Headers;
            headers.TryGetValue("Authorization", out var headerValue);
            string token = headerValue.ToString();
            if (token == null)
            {
                return Unauthorized();
            }

            //Get Person ID
            string[] arg = user.personId.Split('/');
            if (arg[0] == "Person" && arg.Length >= 2)
            {
                loginData["person"]["id"] = arg[1];
            }
            else
            {
                return BadRequest("PersonID not found.");
            }

            //Get Patient ID
            arg = user.patientId.Split('/');
            if (arg[0] == "Patient" && arg.Length >= 2)
            {
                loginData["patient"]["id"] = arg[1];
            }
            else
            {
                return BadRequest("PatientID not found.");
            }

            loginData["schedule"]["id"] = "860";

            //Check Login Data (Person == Patient)
            loginData["errmsg"] = "Check Login Person failed.";
            string param = '/' + loginData["person"]["id"].ToString();
            JObject result = HTTPrequest.getResource(fhirUrl, "Person", param, token, selectCourse.GetUserRole, loginData);

            return Ok(result);
        }

        [HttpPost("api/Login")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult Login([FromForm] IFormCollection user)
        {
            LoginModel loginModel = new LoginModel(_configuration);

            //Get Email
            loginData["person"]["email"] = user["email"].ToString();

            //Get Password
            loginData["person"]["password"] = user["password"].ToString();

            //Verify Email and Password
            loginData["errmsg"] = "Check Login Person failed.";
            string param = "?identifier=" + user["email"].ToString();
            JObject result = HTTPrequest.getResource(fhirUrl, "Person", param, "", loginModel.Check, loginData);

            return Ok(result);
        }

        [HttpPost("api/jwt")]
        public IActionResult JWT([FromBody] JObject data)
        {
            //string d = Newtonsoft.Json.JsonConvert.SerializeObject(data["selectedDocRef"]);
            dynamic docRef = data["selectedDocRef"];

            string docUrl = (docRef["resource"]["content"] != null) ? docRef["resource"]["content"][0]["attachment"]["url"].ToString() : "";

            List<string> endpoints = new List<string>();
            endpoints.Add(docUrl);

            // Audience (who or what the token is intended for)
            int idx = docUrl.IndexOf("fhir/");
            string _aud = docUrl[..idx];

            string userRole = _aud + data["userSelectedRole"].ToString();
            string author = (docRef["resource"]["author"] != null) ? docRef["resource"]["author"][0]["reference"].ToString() : "";


            JWTModel jwtModel = new JWTModel(_configuration);
            //Verify ID Token ("Authorization" Header)

            //Check FHIR Consent (if author != oneself)
            if (userRole != author)
            {

            }

            // Generate Access Token for FHIR Document
            string docToken = JWTModel.GenerateAccessToken(_aud, userRole, docUrl);

            // Get FHIR Document
            JObject result = HTTPrequest.getResource(docUrl, "", "", docToken, null, loginData);

            JObject composition = (JObject)result["entry"][0]["resource"];

            if (composition["resourceType"].ToString() == "Composition")
            {
                foreach (JObject entry in composition["section"][0]["entry"])
                {
                    endpoints.Add(_aud + entry["reference"].ToString());
                }
            }

            //Generate Access Token for FHIR Document & its content
            string allToken = JWTModel.GenerateAccessToken(_aud, userRole, endpoints.ToArray());

            JObject retData = new JObject();
            retData["docURL"] = docUrl;
            
            if (data["viewer"].ToString() == "skinlesion.report.document")
            {
                retData["viewerURL"] = "http://203.64.84.32:9876/viewer";
            }
            else
            {
                retData["viewerURL"] = "";
            }

            retData["accessToken"] = allToken;

            //Return to viewer URL and access token to client
            return Ok(retData);
        }
    }
}
