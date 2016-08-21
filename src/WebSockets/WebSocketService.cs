using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Smallscord.WebSockets
{
	public class WebSocketService
	{
		private ConcurrentDictionary<string, WebSocketController> sessions;
		public WebSocketService()
		{
			sessions = new ConcurrentDictionary<string, WebSocketController>();
		}

		public WebSocketController GetOrCreate(string key, Func<string, WebSocketController> create)
		{
			return sessions.GetOrAdd(key, create);
		}

		public ReconnectStatus TryOverwrite(string key, WebSocketController controller)
		{
			
			WebSocketController existing;
			if (!TryGet(key, out existing))
			{
				GetOrCreate(key, x => controller);
				return ReconnectStatus.InitialConnect;
			}
			else if (existing.Closed)
			{
				if (TryRemove(key, out existing))
				{
					if (GetOrCreate(key, x => controller) == controller)
						return ReconnectStatus.Reconnect;
				}
			}

			return ReconnectStatus.AlreadyConnected;
		}

		public bool TryGet(string key, out WebSocketController value)
		{
			return sessions.TryGetValue(key, out value);
		}

		public bool TryRemove(string key, out WebSocketController value)
		{
			return sessions.TryRemove(key, out value);
		}
	}
}