using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FHIR_LMS_WEBAPI_CORE.Models
{
    public class HTTPrequest
    {
        public JObject getResource(string fhirUrl, string ResourceName, string Parameter, string token, Func<JObject, JObject, string, JObject> CallbackFunction, JObject loginData)
        {
            dynamic errmsg = new JObject();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var requestHttp = (HttpWebRequest)WebRequest.Create(fhirUrl + ResourceName + Parameter);
                requestHttp.ContentType = "application/json";
                requestHttp.Method = "GET";
                requestHttp.Headers["Authorization"] = token;
                var response = (HttpWebResponse)requestHttp.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        JObject resultJson = JObject.Parse(result);

                        if (resultJson["resourceType"].ToString() == "Bundle")
                        {
                            string type = resultJson["type"].ToString();
                            if (type == "searchset" && (int)resultJson["total"] <= 0 && ResourceName!="Appointment")
                            {
                                errmsg.total = 0;
                                errmsg.message = ResourceName + " does not exist.";
                                errmsg.code = 404;
                                return errmsg;
                            }
                        }
                        else if (resultJson["resourceType"].ToString() == "OperationOutcome")
                        {
                            foreach (JObject issue in resultJson["issue"])
                            {
                                errmsg.message += issue["diagnostics"]+ "\n";
                                errmsg.code = 404;
                            }
                                return errmsg;
                        }

                        if (CallbackFunction == null)
                        {
                            return resultJson;
                        }
                        else
                        {
                            return CallbackFunction(resultJson, loginData, token);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                errmsg.message = loginData["errmsg"] + "\n" + e.Message;
                return errmsg;
            }
            errmsg.message = loginData["errmsg"];
            return errmsg;
        }

        public JObject postResource(string fhirUrl, string ResourceName, JObject body, string token, Func<JObject, JObject, string, JObject> CallbackFunction, JObject loginData)
        {
            dynamic errmsg = new JObject();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var requestHttp = (HttpWebRequest)WebRequest.Create(fhirUrl + ResourceName);
                requestHttp.ContentType = "application/json";
                requestHttp.Method = "POST";
                requestHttp.Headers["Authorization"] = token;
                string postBody = body.ToString();
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postBody);
                using (Stream reqStream = requestHttp.GetRequestStream())
                {
                    reqStream.Write(byteArray, 0, byteArray.Length);
                }

                var response = (HttpWebResponse)requestHttp.GetResponse();
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        JObject resultJson = JObject.Parse(result);

                        if (CallbackFunction == null)
                        {
                            return resultJson;
                        }
                        else
                        {
                            return CallbackFunction(resultJson, loginData, token);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                errmsg.message = loginData["errmsg"] + "\n" + e.Message;
                return errmsg;
            }
            errmsg.message = loginData["errmsg"];
            return errmsg;
        }

        public JObject putResource(string fhirUrl, string ResourceName, JObject body, string token, Func<JObject, JObject, string, JObject> CallbackFunction, JObject loginData)
        {
            dynamic errmsg = new JObject();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var requestHttp = (HttpWebRequest)WebRequest.Create(fhirUrl + ResourceName);
                requestHttp.ContentType = "application/json";
                requestHttp.Method = "PUT";
                requestHttp.Headers["Authorization"] = token;
                string putBody = body.ToString();
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(putBody);
                using (Stream reqStream = requestHttp.GetRequestStream())
                {
                    reqStream.Write(byteArray, 0, byteArray.Length);
                }

                var response = (HttpWebResponse)requestHttp.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        JObject resultJson = JObject.Parse(result);
                        if (CallbackFunction != null)
                        {
                            JObject callbackResult = CallbackFunction(resultJson, loginData, token);
                            return callbackResult;
                        }
                        return resultJson;
                    }
                }
            }
            catch (Exception e)
            {
                errmsg.message = loginData["errmsg"] + "\n" + e.Message;
                return errmsg;
            }
            errmsg.message = loginData["errmsg"];
            return errmsg;
        }
    }
}
