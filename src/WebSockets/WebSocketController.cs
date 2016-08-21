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

		private WebSocket socket;
		private WebSocketService service;
		private ILogger<WebSocketController> websocketLogger;

		public WebSocketController(WebSocketService controllerService, WebSocket _socket, ILoggerFactory factory)
		{
			socket = _socket;
			service = controllerService; 
			websocketLogger = factory.CreateLogger<WebSocketController>();
		}

		public bool Connected => socket.State == WebSocketState.Open;
		public bool Closed => socket.State == WebSocketState.Closed || 
			socket.State == WebSocketState.CloseReceived ||
			socket.State == WebSocketState.CloseSent;  

		public async Task Run()
		{
			await SendHello();

			var buffer = new byte[1024 * 4]; // 4kb
			var read = 0;
			while (socket.State == WebSocketState.Open)
			{
				var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, read, 512), CancellationToken.None);

				if (result.EndOfMessage)
				{
					read += result.Count;
					websocketLogger.LogDebug("Received {0} bytes, message type {1}", read, result.MessageType);

					if (result.MessageType == WebSocketMessageType.Text)
					{
						var message = Encoding.UTF8.GetString(buffer, 0, read);
						await HandleClientMessageString(message);
					}
					else if (result.MessageType == WebSocketMessageType.Binary)
					{
						// TODO: handle binary message
					}

					// empty the buffer and reset read offset
					buffer.Initialize();
					read = 0;
				}
				else
				{
					read += result.Count;

					websocketLogger.LogDebug("Read {0} bytes of non-complete message", read);
				}
			}
		}

		public Task SendRaw(byte[] data, WebSocketMessageType type, CancellationToken token)
		{
			return socket.SendAsync(new ArraySegment<byte>(data), type, true, token);
		}
		public Task SendString(string data) => SendString(data, WebSocketMessageType.Text, CancellationToken.None);		
		public Task SendString(string data, CancellationToken token) => SendString(data, WebSocketMessageType.Text, token);
		public Task SendString(string data, WebSocketMessageType type, CancellationToken token) => SendRaw(Encoding.UTF8.GetBytes(data), type, token);

		public Task SendEntity(GatewayEntity entity) => SendEntity(entity, WebSocketMessageType.Text, CancellationToken.None);
		public Task SendEntity(GatewayEntity entity, CancellationToken token) => SendEntity(entity, WebSocketMessageType.Text, token);
		public Task SendEntity(GatewayEntity entity, WebSocketMessageType type, CancellationToken token) => SendString(entity.ToString(), type, token);

		public Task SendClose(ushort reason) => SendClose(reason, CancellationToken.None);
		public Task SendClose(ushort reason, CancellationToken token)
		{
			return socket.CloseAsync((WebSocketCloseStatus)reason, "", token);
		}

		private async Task HandleClientMessageString(string message)
		{
			try
			{
				object data = JsonConvert.DeserializeObject(message);
				var opcode = (data as GatewayEntity).Opcode;

				switch(opcode)
				{
					case GatewayOpcode.Identify:
						websocketLogger.LogInformation("Handling Identify");
						GatewayIdentify loginInfo = data as GatewayIdentify;

						if (string.IsNullOrWhiteSpace(loginInfo.ClientToken))
						{
							websocketLogger.LogWarning("Invalid client token was provided");
							await SendClose(4004);
						}

						if (!service.TryOverwrite(loginInfo.ClientToken, this))
						{
							websocketLogger.LogWarning("A client already exists using the token {0}", loginInfo.ClientToken);
							await SendClose(4005);
						}

						if (loginInfo.ShardInfo.Length > 0 && loginInfo.ShardInfo.Length != 2)
						{
							websocketLogger.LogWarning("Invalid shard info (expected 2 values, got {0})", loginInfo.ShardInfo.Length);
							await SendClose(4010);
						}
						break;
					case GatewayOpcode.Resume:
						websocketLogger.LogInformation("Handling Resume");
						break;
					default:
						websocketLogger.LogWarning("Unknown opcode {0}", opcode);
						await SendClose(4001);
						break;
				}
			}
			catch (JsonException e)
			{
				websocketLogger.LogTrace("Exception occured while deserializing JSON: {0}", e);
				await SendClose(4002);
			}
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