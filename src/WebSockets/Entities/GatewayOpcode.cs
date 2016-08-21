namespace Smallscord.WebSockets.Entities
{
	public enum GatewayOpcode
	{
		Heartbeat = 1,
		Identify = 2,

		Resume = 6,
		
		Hello = 10
	}
}