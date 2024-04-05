using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ResoniteModLoader;

namespace ResoniteHeartRate {
    internal class HeartRateClient {

        public enum errorMessages {
            AUTHENTICATION = 0,
            READ = 1,
            GENERATING_FORMAT = 2,
            CURRENTDATA_FORMAT = 3
        }
        public string GenerateToken(string pulsoidKey) {

            string jsonData = SendHRhttp("https://dev.pulsoid.net/api/v1/token/validate", pulsoidKey, errorMessages.AUTHENTICATION);

            string token = "";

            try {
                foreach (string commas in jsonData.Split(',')) {
                    
                    if (commas.Contains("client_id")) {
                        token = commas.Split('"')[3];

                    }
                }
            }
            catch { ResoniteMod.Error(getErrorMessage(errorMessages.GENERATING_FORMAT)); }

            return token;
        }

        public int ReadCurrentHR(string Key) {

            string jsonData = SendHRhttp("https://dev.pulsoid.net/api/v1/data/heart_rate/latest", Key, errorMessages.READ);
            int HeartRate = 0;
            try {
                foreach (string commas in jsonData.Split(',')) {
                    if (commas.Contains("heart_rate")) {
                        string heartRateStr = commas.Split(':')[2].Split('}')[0];

                        int.TryParse(heartRateStr, out HeartRate);
                    }
                }
            }
            catch { 
                ResoniteMod.Error(getErrorMessage(errorMessages.CURRENTDATA_FORMAT));
                HeartRate = 0;
            }

            return HeartRate;
        }

        private static string getErrorMessage(errorMessages ErrorCode) {


            string message;

            switch (ErrorCode) {
                case errorMessages.AUTHENTICATION:
                default:

                    message = "Heartrate Error: Authenticating token failed";
                    break;
                case errorMessages.READ:

                    message = "Heartrate Error: Could not Read heartrate";
                    break;
                case errorMessages.GENERATING_FORMAT:

                    message = "Heartrate Error: Sending Pulsoid Key did not return correct Data";
                    break;
                case errorMessages.CURRENTDATA_FORMAT:

                    message = "Heartrate Error: Trying to get current HeartRate did not return correct Data";
                    break;


            }

            return message;
        }

        public String SendHRhttp(string urlStr, string KEY, errorMessages errorCode, string method = "GET", string contentType = "application/json") {

            var url = new Uri(urlStr);
            var req = (HttpWebRequest)WebRequest.Create(url);

            req.Method = method;
            req.Headers["Authorization"] = "Bearer " + KEY;
            req.ContentType = contentType;


            req.UseDefaultCredentials = true;
            req.PreAuthenticate = true;
            req.Credentials = CredentialCache.DefaultCredentials;

            string jsonStr = "";

            try {

                var Response = (HttpWebResponse)req.GetResponse();

                var streaReader = new StreamReader(Response.GetResponseStream());

                jsonStr = streaReader.ReadLine();

            }

            catch { 
                ResoniteMod.Error(getErrorMessage(errorCode));
                jsonStr = "";
            }

            return jsonStr;
        }
    }
}
