using Newtonsoft.Json;

namespace Smallscord.WebSockets.Entities
{
	public class GatewayIdentify : GatewayEntity
	{
		public GatewayIdentify()
		{
			Opcode = GatewayOpcode.Identify;
		}

		[JsonProperty("token")]
		public string ClientToken { get; set; }
		
		[JsonProperty("properties")]
		public ClientProperties Properties { get; set; }
		[JsonPropertyAttribute("compress")]
		public bool CompressionEnabled { get; set; }
		[JsonProperty("large_threshold")]
		public int MaxUsers { get; set; }
		[JsonPropertyAttribute("shard")]
		public int[] ShardInfo { get; set; }
	}

	public class ClientProperties
	{
		[JsonProperty("$os")]
		public string OperatingSystem { get; set; }
		[JsonProperty("$browser")]
		public string Browser { get; set; }
		[JsonProperty("$device")]
		public string Device { get; set; }
		[JsonProperty("$referrer")]
		public string Referrer { get; set; }
		[JsonProperty("$referring_domain")]
		public string ReferringDomain { get; set; }
	}
}