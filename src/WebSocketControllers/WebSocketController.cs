using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Smallscord.WebSocketControllers
{
	// OKAY. I KNOW SINGLETONS ARE BAD BUT ONLY ONE CLIENT WILL EXIST AT ANY TIME SINCE THIS IS LOCAL.
	class WebSocketController
	{
		private static WebSocketController instance;

		private WebSocket _socket;
		private ILogger<WebSocketController> websocketLogger;

		public WebSocketController(WebSocket socket, ILoggerFactory factory)
		{
			if (instance != null)
				throw new InvalidOperationException("An instance already exists");
				
			_socket = socket;
			websocketLogger = factory.CreateLogger<WebSocketController>();
			instance = this;
		}

		public async Task Run()
		{
			var buffer = new byte[1024 * 8]; // 8kb
			var read = 0;
			while (_socket.State == WebSocketState.Open)
			{
				var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer, read, 1024 * 4), CancellationToken.None);
				if (result.EndOfMessage)
				{
					read += result.Count;
					// handle complete message
					var message = Encoding.UTF8.GetString(buffer, 0, read);
					await Handle(message);
					buffer.Initialize(); // empty the buffer
					read = 0;
				}
				else
				{
					read += result.Count;
				}
			}
		}

		private async Task Handle(string message)
		{
			// TODO: handle the message
			websocketLogger.LogInformation(
				"Received payload: `{0}`",
				message
			);
		} 

		public static WebSocketController GetInstance()
		{
			return instance;
		}
	}
}