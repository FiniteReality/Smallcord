using Newtonsoft.Json;

namespace Smallscord.WebSockets.Entities
{
	public class GatewayEntity
	{
		public GatewayEntity(GatewayOpcode opcode)
		{
			Opcode = opcode;
		}

		[JsonProperty("op")]
		public GatewayOpcode Opcode { get; set; }
		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}