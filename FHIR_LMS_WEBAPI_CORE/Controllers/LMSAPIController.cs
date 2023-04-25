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
        public LMSAPIController(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
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

            HTTPrequest HTTPrequest = new HTTPrequest();
            JObject loginData = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "LoginData.json")));


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

            HTTPrequest HTTPrequest = new HTTPrequest();
            var section = _configuration.GetSection("UserLoginJSON");
            JObject loginData = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "LoginData.json")));

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
    }
}
