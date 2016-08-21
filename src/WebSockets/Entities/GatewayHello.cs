using Newtonsoft.Json;

namespace Smallscord.WebSockets.Entities
{
	public class GatewayHello : GatewayEntity
	{
		public GatewayHello()
			: base (GatewayOpcode.Hello)
		{
		}

		[JsonProperty("heartbeat_interval")]
		public int HeartbeatInterval { get; set; }
		[JsonProperty("_trace")]
		public string[] ConnectedServers { get; set; }
	}
}