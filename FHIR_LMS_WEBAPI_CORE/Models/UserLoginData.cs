using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class UserLoginData
    {
        public string email { get; set; }
        public string password { get; set; }

        
    }
}
