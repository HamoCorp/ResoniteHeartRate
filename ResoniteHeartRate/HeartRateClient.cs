using System.Net;

using ResoniteModLoader;

namespace ResoniteHeartRate;

internal class HeartRateClient {
	public int TimerCount = 0;

	public enum HRService {
		Pulsoid,
		HypeRate,
		Debug_Values
	}
	public enum ErrorMessages {
		AUTHENTICATION = 0,
		READ = 1,
		GENERATING_FORMAT = 2,
		CURRENTDATA_FORMAT = 3
	}
	public string HeartRateInit(string Key, HRService service = HRService.Pulsoid) {
		string token = "";

		if (service == HRService.Pulsoid) {
			string jsonData = SendHRhttpPulsoid("https://dev.pulsoid.net/api/v1/token/validate", Key, ErrorMessages.AUTHENTICATION);

			try {
				foreach (string commas in jsonData.Split(',')) {

					if (commas.Contains("client_id")) {
						token = commas.Split('"')[3];

					}
				}
			} catch {
				ResoniteMod.Error(GetErrorMessage(ErrorMessages.GENERATING_FORMAT));
			}

			return token;
		} else if (service == HRService.HypeRate) {
			return "";
		} else {
			return "";
		}
	}

	public int ReadCurrentHR(string Key, HRService service = HRService.Pulsoid) {
		int HeartRate = 0;

		if (service == HRService.Pulsoid) {
			string jsonData = SendHRhttpPulsoid("https://dev.pulsoid.net/api/v1/data/heart_rate/latest", Key, ErrorMessages.READ);

			try {
				foreach (string commas in jsonData.Split(',')) {
					if (commas.Contains("heart_rate")) {
						string heartRateStr = commas.Split(':')[2].Split('}')[0];

						int.TryParse(heartRateStr, out HeartRate);
					}
				}
			} catch {
				ResoniteMod.Error(GetErrorMessage(ErrorMessages.CURRENTDATA_FORMAT));
				HeartRate = 0;
			}

			return HeartRate;

		} else if (service == HRService.HypeRate) {
			return 69;
		} else {
			return (TimerCount * 3) + 60;
		}
	}

	public void HypeRateKeep_Alive() { }

	private static string GetErrorMessage(ErrorMessages ErrorCode) {
		string message = ErrorCode switch {
			ErrorMessages.READ => "Heartrate Error: Could not Read heartrate",
			ErrorMessages.GENERATING_FORMAT => "Heartrate Error: Sending Pulsoid Key did not return correct Data",
			ErrorMessages.CURRENTDATA_FORMAT => "Heartrate Error: Trying to get current HeartRate did not return correct Data",
			_ => "Heartrate Error: Authenticating token failed",
		};
		return message;
	}

	public string SendHRhttpPulsoid(string urlStr, string KEY, ErrorMessages errorCode, string method = "GET", string contentType = "application/json") {
		Uri url = new(urlStr);
		var req = (HttpWebRequest)WebRequest.Create(url);

		req.Method = method;
		req.Headers["Authorization"] = "Bearer " + KEY;
		req.ContentType = contentType;


		req.UseDefaultCredentials = true;
		req.PreAuthenticate = true;
		req.Credentials = CredentialCache.DefaultCredentials;

		string jsonStr;

		try {

			var Response = (HttpWebResponse)req.GetResponse();

			var streaReader = new StreamReader(Response.GetResponseStream());

			jsonStr = streaReader.ReadLine();

		} catch {
			ResoniteMod.Error(GetErrorMessage(errorCode));
			jsonStr = "";
		}

		return jsonStr;
	}

	//https://github.com/HypeRate/DevDocs
	public string HypeRateWebsocet(string urlStr, string topic, ErrorMessages errorCode, string Event = "GET") {
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

		string jsonStr;

		try {
			var Response = (HttpWebResponse)req.GetResponse();

			var streaReader = new StreamReader(Response.GetResponseStream());

			jsonStr = streaReader.ReadLine();
		} catch {
			ResoniteMod.Error(GetErrorMessage(errorCode));
			jsonStr = "";
		}

		return jsonStr;
	}
}
