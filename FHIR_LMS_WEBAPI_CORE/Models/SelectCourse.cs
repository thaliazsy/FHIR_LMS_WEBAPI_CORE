using Newtonsoft.Json.Linq;
using System.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class SelectCourse
    {
        private readonly IConfiguration _configuration;
        private readonly string fhirUrl;
        HTTPrequest HTTPrequest;

        public SelectCourse(IConfiguration configuration)
        {
            _configuration = configuration;
            fhirUrl = _configuration.GetValue<string>("TZFHIR_Url");
            HTTPrequest = new HTTPrequest();
        }

        public JObject GetUserRole(JObject personUser, JObject loginData, string token)
        {

            dynamic result = null;
            loginData["person"]["id"] = personUser["id"] != null ? personUser["id"] : "";
            loginData["person"]["name"] = personUser["name"][0]["text"] != null ? personUser["name"][0]["text"] : "";
            loginData["person"]["identifier"] = personUser["identifier"][0] != null ? personUser["identifier"][0]["value"] : "";

            if (personUser["link"] != null)
            {
                foreach (JObject role in (JArray)personUser["link"])
                {
                    string roleID = role["target"]["reference"].ToString();

                    string param = string.Empty;

                    if (roleID.Split('/')[0] == "Practitioner")
                    {
                        param = "?practitioner=" + roleID.Split('/')[1];
                        result = HTTPrequest.getResource(fhirUrl, "PractitionerRole", param, token, GetSchedule, loginData);
                        return result;
                    }
                    else if (roleID.Split('/')[0] == "Patient")
                    {
                        if (loginData["patient"]["id"].ToString() == roleID.Split('/')[1].ToString())
                        {
                            loginData["errmsg"] = "GET Patient failed.";
                            param = '/' + loginData["patient"]["id"].ToString();
                            result = HTTPrequest.getResource(fhirUrl, "Patient", param, token, GetSchedule, loginData);
                            return result;
                        }
                    }
                }
                result["message"] = "Patient does not belong to this Person.";
            }
            return result;
        }

        public JObject GetSchedule(JObject patientUser, JObject loginData, string token)
        {
            //GET Schedule -> get CourseID
            loginData["errmsg"] = "GET Schedule failed.";
            string param = '/' + loginData["schedule"]["id"].ToString();
            JObject result = HTTPrequest.getResource(fhirUrl, "Schedule", param, token, GetSlotID, loginData);
            return result;
        }

        public JObject GetSlotID(JObject schedule, JObject loginData, string token)
        {

            loginData["schedule"]["courseCode"] = schedule["specialty"][0]["coding"][0]["code"].ToString();
            loginData["schedule"]["name"] = schedule["name"];

            //GET SlotID
            loginData["errmsg"] = "GET Slot failed.";
            string param = "?schedule=" + loginData["schedule"]["id"];
            JObject result = HTTPrequest.getResource(fhirUrl, "Slot", param, token, CheckAppointments, loginData);
            return result;
        }

        public JObject CheckAppointments(JObject slot, JObject loginData, string token)
        {
            HTTPrequest HTTPrequest = new HTTPrequest();

            loginData["slot"]["id"] = slot["entry"][0]["resource"]["id"];

            //Check Appointments
            loginData["errmsg"] = "GET Appointments failed.";
            string param = "?slot=" + loginData["slot"]["id"].ToString() + "&patient=" + loginData["patient"]["id"].ToString();
            JObject result = HTTPrequest.getResource(fhirUrl, "Appointment", param, token, null, loginData);

            if ((string)result["resourceType"] == "Bundle" && (string)result["type"] == "searchset" && result["entry"] != null) //Person not found
            {
                JObject errmsg = new JObject();
                errmsg["errmsg"] = "You have resgitered this course.";
                return errmsg;
            }
            else
            {
                CreateAppointment(slot, loginData, token);
            }
            return result;
        }

        public JObject CreateAppointment(JObject slot, JObject loginData, string token)
        {
            HTTPrequest HTTPrequest = new HTTPrequest();

            loginData["slot"]["id"] = slot["entry"][0]["resource"]["id"];

            //Create Appointment Waitlist
            JObject appointment = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Assets/JSON/Appointment.json")));
            appointment["participant"][0]["actor"]["reference"] = "Patient/" + loginData["patient"]["id"];
            appointment["participant"][0]["actor"]["display"] = loginData["person"]["name"];
            appointment["slot"][0]["reference"] = "Slot/" + loginData["slot"]["id"];
            appointment["status"] = "waitlist";

            //POST new Appointment
            loginData["errmsg"] = "Create Appointment failed.";
            JObject result = HTTPrequest.postResource(fhirUrl, "Appointment", appointment, token, GetGroupQuantity, loginData);

            return result;
        }

        public JObject GetGroupQuantity(JObject appointment, JObject loginData, string token)
        {
            HTTPrequest HTTPrequest = new HTTPrequest();

            loginData["appointment"]["id"] = appointment["id"];

            //GET Group -> identifier=coursecode
            loginData["errmsg"] = "GET Group quantity failed.";
            string param = "?identifier=" + loginData["schedule"]["courseCode"];
            JObject result = HTTPrequest.getResource(fhirUrl, "Group", param, token, GetBookedAppointment, loginData);

            return result;
        }
        public JObject GetBookedAppointment(JObject group, JObject loginData, string token)
        {
            HTTPrequest HTTPrequest = new HTTPrequest();

            //GET maximum participant
            loginData["schedule"]["maxParticipant"] = group["entry"][0]["resource"]["quantity"] != null ? (int)group["entry"][0]["resource"]["quantity"] : 0;

            //GET Appointment -> slot id
            //GET "Booked" Appointment Quantity
            loginData["errmsg"] = "GET booked Appointment failed.";
            string param = "?slot=" + loginData["slot"]["id"] + "&status=booked";
            JObject result = HTTPrequest.getResource(fhirUrl, "Appointment", param, token, GetWaitlistAppointment, loginData);

            return result;

        }
        public JObject GetWaitlistAppointment(JObject appSearch, JObject loginData, string token)
        {
            JObject result = new JObject();
            HTTPrequest HTTPrequest = new HTTPrequest();

            loginData["schedule"]["currentParticipant"] = appSearch["total"];

            int groupQty = (int)loginData["schedule"]["maxParticipant"];
            int bookedQty = (int)appSearch["total"];

            if (bookedQty < groupQty)
            {
                int diff = groupQty - bookedQty;

                //GET "Waitlist" Appointments, Sort updated
                string param = "?slot=" + loginData["slot"]["id"] + "&status=waitlist";
                result = HTTPrequest.getResource(fhirUrl, "Appointment", param, token, CheckCourseAvailability, loginData);
                return result;
            }
            //Alert maximum course
            result["message"] = "This course has reached its maximum capacity. " +
                "We have added your name into the waiting list." +
                "You'll be able to see the course material once approved by admin.";
            return result;
        }

        public JObject CheckCourseAvailability(JObject appSearch, JObject loginData, string token)
        {
            JObject result = new JObject();
            HTTPrequest HTTPrequest = new HTTPrequest();

            int diff = (int)loginData["schedule"]["maxParticipant"] - (int)loginData["schedule"]["currentParticipant"];
            int waitlistQty = (int)appSearch["total"];

            //diff = diff < waitlistQty ? diff : waitlistQty;

            string appointmentID = loginData["appointment"]["id"].ToString();
            if (appSearch["entry"] != null)
            {
                var entry = JArray.Parse(appSearch["entry"].ToString());
                var requiredArticle = entry.First(a => a["resource"]["id"].ToString().Equals(appointmentID));
                int index = entry.IndexOf(requiredArticle);

                if (index != -1 && index < diff)
                {
                    JObject new_appointment = (JObject)appSearch["entry"][index]["resource"];
                    new_appointment.Property("meta").Remove();
                    new_appointment["status"] = "booked";

                    string param = '/' + appointmentID;
                    result = HTTPrequest.putResource(fhirUrl, "Appointment" + param, new_appointment, token, null, loginData);
                    return result;

                }
            }

            //Alert maximum course
            result["message"] = "This course has reached its maximum capacity. " +
                "We have added your name into the waiting list." +
                "You'll be able to see the course material once approved by admin.";
            return result;
        }
    }
}
