using Newtonsoft.Json;

using Smallscord.Data;

namespace Smallscord.Events
{
	public class EventReady : GatewayEvent
	{
		public EventReady(string sessionId)
			: base (EventType.Ready)
		{
			SessionId = sessionId;
		}
		
		[JsonProperty("v")]
		public int GatewayVersion { get; set; }
		[JsonProperty("user")]
		public UserInfo User { get; set; }
		[JsonProperty("session_id")]
		public string SessionId { get; set; }
	}
}