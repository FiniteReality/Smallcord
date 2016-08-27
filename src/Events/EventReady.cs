using Newtonsoft.Json;

using Smallscord.Data;

namespace Smallscord.Events
{
	public class EventReady : GatewayEvent
	{
		public const int GATEWAY_VERSION = 6;

		public EventReady(string sessionId)
			: base (EventType.Ready)
		{
			SessionId = sessionId;
			User = UserInfo.Default;
			GatewayVersion = GATEWAY_VERSION;
		}
		
		[JsonProperty("v")]
		public int GatewayVersion { get; set; }
		[JsonProperty("user")]
		public UserInfo User { get; set; }
		[JsonProperty("session_id")]
		public string SessionId { get; set; }
	}
}