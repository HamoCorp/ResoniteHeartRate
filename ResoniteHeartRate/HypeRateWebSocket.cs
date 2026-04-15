using ResoniteModLoader;

using WebSocketSharp;

using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace ResoniteHeartRate;

internal class HypeRateWebSocket {

	public HypeRateWebSocket(string HypeRateID) {
		string url = "wss://app.hyperate.io/socket/websocket?token=" + SECRET_UNIQUE_HYPERATE_API_KEY;

		_ws = new WebSocket(url);

		// Required: force modern TLS
		_ws.SslConfiguration.EnabledSslProtocols =
			System.Security.Authentication.SslProtocols.Tls12
			| System.Security.Authentication.SslProtocols.Tls12;

		// Debug logging
		_ws.OnError += (s, e) => {
			ResoniteMod.Error($"[HypeRate WS ERROR] {e.Message}\n{e.Exception}");
		};

		_ws.OnClose += (s, e) => {
			ResoniteMod.Error($"[HypeRate WS CLOSED] Code={e.Code}, Reason={e.Reason}");
		};

		_ws.OnMessage += Ws_OnMessage;

		try {
			_ws.Connect();
		} catch (Exception ex) {
			ResoniteMod.Error("[HypeRate] Exception during Connect(): " + ex);
			return;
		}

		if (!_ws.IsAlive) {
			ResoniteMod.Error("[HypeRate] WebSocket failed to connect.");
			return;
		}

		// Only send join message AFTER a successful connect
		string joinMsg = new HypeRateJson("hr:" + HypeRateID, "phx_join", "", "0").ToJson();
		_ws.Send(joinMsg);
	}

	public void SendKeepAlive() {
		_ws.Send(HypeRateJson.KeepAliveMessage());
	}

	private static void Ws_OnError(object sender, ErrorEventArgs e) {
		ResoniteMod.Error("Hype rate websocket Did not return valid stuff: " + e.ToString());
	}

	private static void Ws_OnMessage(object sender, MessageEventArgs e) {
		try {
			HypeRateJson hj = new(e.Data);
			string evnt = hj.GetEvent();

			switch (evnt) {
				case "hr_update":
					_heartRate = hj.GetHeartRate();
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

	public int GetHypeRateHeartRate() { return _heartRate; }

	public bool GetHypeRateAlive() { return _HypeRateIsAlive; }
	public void SetHypeRateAliveOnLoop() { _HypeRateIsAlive = false; }

	//my Secret asigned api key from https://www.hyperate.io/api
	private const string SECRET_UNIQUE_HYPERATE_API_KEY = "dbbxSOFxzN9ySSrz53eXXtJIQjMZ3ZIOJfMV6fG9J4jbjn9vJD2vsFm7rYqrUgs3";

	private static WebSocket _ws;
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
