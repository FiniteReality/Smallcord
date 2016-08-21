using Newtonsoft.Json;

namespace Smallscord.WebSockets.Entities
{
	public class GatewayResume : GatewayEntity
	{
		public GatewayResume()
		{
			Opcode = GatewayOpcode.Resume;
		}

		[JsonProperty("token")]
		public string ClientToken { get; set; }
		
		[JsonProperty("session_id")]
		public string SessionId { get; set; }

		[JsonPropertyAttribute("seq")]
		public int SequenceNumber { get; set; }
	}
}