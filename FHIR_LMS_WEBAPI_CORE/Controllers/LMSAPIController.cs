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
            loginData = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Assets/JSON/LoginData.json")));
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        [HttpPost("api/select-course")]
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

        [HttpPost("api/login")]
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
            if ((int)result["code"] != 200)
            {
                return Ok("Incorrect email or password.");
            }
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
            string docToken = jwtModel.GenerateAccessToken(_aud, userRole, docUrl);

            // Get FHIR Document
            JObject result = HTTPrequest.getResource(docUrl, "", "", docToken, null, loginData);

            if (result["message"] != null)
            {
                //Return to viewer URL and access token to client
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
            if (result["entry"] == null || result["entry"][0]["resource"] == null)
            {
                //Return to viewer URL and access token to client
                return StatusCode(StatusCodes.Status500InternalServerError, "No document entry available.");
            }

            foreach (JObject entry in result["entry"])
            {
                if (entry["resource"]["resourceType"].ToString() == "Composition")
                {
                    JObject composition = (JObject)entry["resource"];
                    foreach (JObject e in composition["section"][0]["entry"])
                    {
                        endpoints.Add(_aud + e["reference"].ToString());
                    }
                    break;
                }
            }

            //Generate Access Token for FHIR Document & its content
            string allToken = jwtModel.GenerateAccessToken(_aud, userRole, endpoints.ToArray());

            JObject retData = new JObject();
            retData["docURL"] = docUrl;

            if (data["viewer"].ToString() == "skinlesion.report.document")
            {
                retData["viewerURL"] = "http://203.64.84.32:9876/viewer/skin-lesion-viewer";
            }
            else if (data["viewer"].ToString() == "skinlesion.image.document")
            {
                retData["viewerURL"] = "http://203.64.84.32:9876/skinlesionimage-ms/ReportCreator?documentbundle=" + docUrl;
                //http://203.64.84.32:9876/ReportCreator?documentbundle=http://203.64.84.32:9876/fhir/Bundle/688
            }
            else
            {
                retData["viewerURL"] = "";
                return BadRequest("Viewer not available.");
            }

            retData["accessToken"] = allToken;

            //Return to viewer URL and access token to client
            //string redirectUrl = "http://203.64.84.32:9876/viewer?wantedDoc=" + docUrl;
            //return RedirectPermanent(redirectUrl);
            return Ok(retData);
        }
    }
}
