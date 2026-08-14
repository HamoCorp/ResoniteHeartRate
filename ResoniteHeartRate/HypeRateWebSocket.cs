using ResoniteModLoader;

namespace ResoniteHeartRate;

using WatsonWebsocket;
internal class HypeRateWebSocket {

	public HypeRateWebSocket(string HypeRateID) {
		Uri url = new Uri("wss://app.hyperate.io/socket/websocket?token=" + SECRET_UNIQUE_HYPERATE_API_KEY);

		_ws = new WatsonWsClient(url);

		_ws.ServerConnected += ServerConnected;
		_ws.ServerDisconnected += ServerDisconnected;
		_ws.MessageReceived += MessageReceived;

		try {
			_ws.Start();
		} catch (Exception ex) {
			ResoniteMod.Error("[HypeRate] Exception during Start(): " + ex);
			return;
		}

		string joinMsg = new HypeRateJson("hr:" + HypeRateID, "phx_join", "", "0").ToJson();

		_ws.SendAsync(joinMsg);
	}

	public void SendKeepAlive() {
		_ws?.SendAsync(HypeRateJson.KeepAliveMessage());
	}

	private void ServerConnected(object sender, EventArgs e) {
		ResoniteMod.Msg("[HypeRate] Connected");
	}

	private void ServerDisconnected(object sender, EventArgs e) {
		ResoniteMod.Warn("[HypeRate] Disconnected");
	}

	private void MessageReceived(object sender, MessageReceivedEventArgs e) {
		try {
			string data = System.Text.Encoding.UTF8.GetString(e.Data);

			HypeRateJson hj = new(data);
			string evnt = hj.GetEvent();

			switch (evnt) {
				case "hr_update":
					_heartRate = hj.GetHeartRate();
					break;

				case "heartbeat":
					_HypeRateIsAlive = true;
					break;

				case "phx_reply":
					_HypeRateIsAlive = true;
					break;

				default:
					ResoniteMod.Error("Unknown HypeRate event: " + evnt);
					break;
			}
		} catch (Exception ex) {
			ResoniteMod.Error("[HypeRate] Error processing message: " + ex);
		}
	}

	public int GetHypeRateHeartRate() => _heartRate;

	public bool GetHypeRateAlive() => _HypeRateIsAlive;

	public void SetHypeRateAliveOnLoop() {
		_HypeRateIsAlive = false;
	}

	//my Secret asigned api key from https://www.hyperate.io/api
	private const string SECRET_UNIQUE_HYPERATE_API_KEY = "dbbxSOFxzN9ySSrz53eXXtJIQjMZ3ZIOJfMV6fG9J4jbjn9vJD2vsFm7rYqrUgs3";

	private static WatsonWsClient _ws;
	private static bool _HypeRateIsAlive = false;
	private static int _heartRate = 0;
	public class HypeRateJson {
		public HypeRateJson(string Topic, string Event, string Payload, string Ref) {
			this._topic = Topic;
			this._event = Event;
			this._payload = Payload;
			this._ref = Ref;
		}

		public HypeRateJson(string JsonData) {
			try {
				foreach (string commas in JsonData.Split(',')) {
					if (commas.Contains("event")) {
						_event = commas.Split('"')[3];
					} else try {
							if (commas.Contains("payload")) {
								_payload = commas.Split(':')[2].Split('}')[0];
								int.TryParse(_payload, out _HeartRate);
								if (_payload.Contains('{')) {
									_HeartRate = 0;
									_payload = "keep-alive packet";
								}
							}
						} catch {
							_payload = "{}";
							_HeartRate = 0;
						}
				}
			} catch {
				ResoniteMod.Error("not correct hyperate format recived");
			}
		}
		public string ToJson() {
			const char q = '"';

			string json1 = q + "topic" + q + ": " + q + _topic + q + ",";
			string json2 = q + "event" + q + ": " + q + _event + q + ",";
			string json3 = q + "payload" + q + ": {" + _payload + "},";
			string json4 = q + "ref" + q + ": " + _ref;
			return "{" + json1 + json2 + json3 + json4 + "}";
		}

		public static string KeepAliveMessage() {
			return new HypeRateJson("phoenix", "heartbeat", "", "0").ToJson();
		}

		public string GetEvent() { return _event; }
		public int GetHeartRate() { return _HeartRate; }

		private string _topic;
		private string _event;
		private string _payload;
		private string _ref;
		private int _HeartRate;

	}
}
