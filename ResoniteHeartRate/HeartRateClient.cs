using System;
using System.IO;
using System.Net;
using ResoniteModLoader;

namespace ResoniteHeartRate {
    internal class HeartRateClient {

        public int TimerCount = 0;
        
        public enum HRService {
            Pulsoid,
            HypeRate,
            Debug_Values
        }
        public enum errorMessages {
            AUTHENTICATION = 0,
            READ = 1,
            GENERATING_FORMAT = 2,
            CURRENTDATA_FORMAT = 3
        }
        public string HeartRateInit(string Key, HRService service = HRService.Pulsoid) {

            string token = "";

            if (service == HRService.Pulsoid) {

                string jsonData = SendHRhttpPulsoid("https://dev.pulsoid.net/api/v1/token/validate", Key, errorMessages.AUTHENTICATION);

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
            else if (service == HRService.HypeRate) {
                return "";
            }
            else {
                return "";
            }

        }

        public int ReadCurrentHR(string Key, HRService service = HRService.Pulsoid) {

            int HeartRate = 0;

            if (service == HRService.Pulsoid) {
                string jsonData = SendHRhttpPulsoid("https://dev.pulsoid.net/api/v1/data/heart_rate/latest", Key, errorMessages.READ);
                
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

            }else if (service == HRService.HypeRate) {
                return HeartRate = 69;
            }

            else {
                return (TimerCount*3) + 60;
            }
        }

        public void HypeRateKeep_Alive() {

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

        public String SendHRhttpPulsoid(string urlStr, string KEY, errorMessages errorCode, string method = "GET", string contentType = "application/json") {

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

        //https://github.com/HypeRate/DevDocs
        public String HypeRateWebsocet(string urlStr, string topic, errorMessages errorCode, string Event = "GET") {

            var url = new Uri(urlStr);
            var req = (HttpWebRequest)WebRequest.Create(url);

            //req.Method = method;
            req.Headers["topic"] = "hr " + topic;
            req.Headers["event"] = Event;
            req.Headers["payload"] = "{}";
            req.Headers["ref"] = "Bearer ";
            //req.ContentType = contentType;


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
