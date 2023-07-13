using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class UserLogin
    {
        public string patientId { get; set; }
        public string personId { get; set; }

        public string scheduleId { get; set; }
    }
}
