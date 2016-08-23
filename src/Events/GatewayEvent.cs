namespace Smallscord.Events
{
	public abstract class GatewayEvent
	{
		internal GatewayEvent(EventType type)
		{
			Event = type;
		}
		public EventType Event { get; }
	}
}