using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Smallscord.WebSockets.Entities;

namespace Smallscord.WebSockets
{
	public class WebSocketController
	{
		private const int HEARTBEAT_INTERVAL = 1000;

		private WebSocket _socket;
		private ILogger<WebSocketController> websocketLogger;

		public WebSocketController(WebSocket socket, ILoggerFactory factory)
		{
			_socket = socket;
			websocketLogger = factory.CreateLogger<WebSocketController>();
		}

		public async Task Run()
		{
			await SendHello();

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

		public Task SendRaw(string data) => SendRaw(data, CancellationToken.None);		
		public async Task SendRaw(string data, CancellationToken token)
		{
			byte[] _data = Encoding.UTF8.GetBytes(data);
			await _socket.SendAsync(new ArraySegment<byte>(_data), WebSocketMessageType.Text, true, token);
		}
		public Task SendEntity(GatewayEntity entity) => SendRaw(entity.ToString());
		public Task SendEntity(GatewayEntity entity, CancellationToken token) => SendRaw(entity.ToString(), token);

		private async Task Handle(string message)
		{
			websocketLogger.LogInformation(
				"Received payload: `{0}`",
				message
			);

			var opcode = JsonConvert.DeserializeObject<GatewayEntity>(message).Opcode;
		}

		public async Task SendHello()
		{
			await SendEntity(new GatewayHello()
			{
				HeartbeatInterval = HEARTBEAT_INTERVAL,
				ConnectedServers = new string[]{"smallscord-gateway-dbg-1-0"}
			});
		}
	}
}