using Newtonsoft.Json;

namespace Smallscord.WebSockets.Entities
{
	public class GatewayDispatch : GatewayEntity
	{
		public GatewayDispatch()
			 : base (GatewayOpcode.Resume)
		{
		}

		[JsonProperty("d")]
		public object Payload { get; set; }
		
		[JsonProperty("s")]
		public int s { get; set; }

		[JsonProperty("t")]
		public string EventName { get; set; }
	}
}