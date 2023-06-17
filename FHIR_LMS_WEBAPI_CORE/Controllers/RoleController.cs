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
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string fhirUrl;
        private JObject loginData;
        private HTTPrequest HTTPrequest = new HTTPrequest();
        public RoleController(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
            loginData = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Assets/JSON/LoginData.json")));
        }

        [HttpGet("mini-apps")]
        public IActionResult MiniApps([FromQuery] string userRole)
        {
            // Get Role
            
            JObject apps=null;

            if (userRole == "PractitionerRole")
            {
                apps = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Assets/JSON/PractRole_Apps.json")));
            }
            else if (userRole == "Patient")
            {
                apps = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Assets/JSON/Patient_Apps.json")));
            }

            return Ok(apps);
        }
    }
}
