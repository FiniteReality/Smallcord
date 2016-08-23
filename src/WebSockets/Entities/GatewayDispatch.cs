using Newtonsoft.Json;

using Smallscord.Events;

namespace Smallscord.WebSockets.Entities
{
	public class GatewayDispatch : GatewayEntity
	{
		public GatewayDispatch(int sequence, GatewayEvent eventInfo)
			 : base (GatewayOpcode.Dispatch)
		{
			Sequence = sequence;
			EventName = eventInfo.Event.GetEventName();
			Payload = eventInfo;
		}

		[JsonProperty("d")]
		public GatewayEvent Payload { get; set; }
		
		[JsonProperty("s")]
		public int Sequence { get; set; }

		[JsonProperty("t")]
		public string EventName { get; set; }
	}
}