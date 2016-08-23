using System.Collections.Generic;

namespace Smallscord.Events
{
	public enum EventType
	{
		Ready = 0
	}
	public static class EventTypeExtensions
	{
		private static Dictionary<EventType, string> EventNames = new Dictionary<EventType, string>(){
			[EventType.Ready] = "READY"
		};
		/// <summary> Returns the Discord name for this event </summary>
		public static string GetEventName(this EventType type)
		{
			return EventNames[type];
		}
	}
}