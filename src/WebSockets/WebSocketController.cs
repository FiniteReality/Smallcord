using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Smallscord.WebSockets.Entities;
using Smallscord.Events;

namespace Smallscord.WebSockets
{
	public class WebSocketController
	{
		private const int HEARTBEAT_INTERVAL = 1000;

		private WebSocket socket;
		private WebSocketService service;
		private ILogger<WebSocketController> websocketLogger;

		private IList<GatewayDispatch> events;

		public WebSocketController(WebSocketService controllerService, WebSocket _socket, ILoggerFactory factory)
		{
			socket = _socket;
			service = controllerService;
			SessionId = Environment.TickCount.ToString();
			websocketLogger = factory.CreateLogger<WebSocketController>();
			events = new List<GatewayDispatch>();
		}

		public bool Connected => socket.State == WebSocketState.Open;
		public bool Closed => socket.State == WebSocketState.Closed || 
			socket.State == WebSocketState.CloseReceived ||
			socket.State == WebSocketState.CloseSent;

		public int Sequence { get; internal set; }
		public string SessionId { get; }

		public async Task Run()
		{
			await SendHello();

			var buffer = new byte[4096];
			var read = 0;
			int lastCheck = 0;
			int eventsSent = 0;
			while (socket.State == WebSocketState.Open)
			{
				var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, read, 4096), CancellationToken.None);

				if (result.EndOfMessage)
				{
					read += result.Count;
					websocketLogger.LogDebug("Received {0} bytes, message type {1}", read, result.MessageType);

					// TODO: implement better ratelimiting
					if (Environment.TickCount - lastCheck > 60000)
					{
						if (eventsSent > 120)
						{
							websocketLogger.LogWarning("Disconnecting client for surpassing rate limit");
							await SendClose(4008);
							break;
						}

						eventsSent = 0;	
						lastCheck = Environment.TickCount;
					}

					if (result.MessageType == WebSocketMessageType.Text)
					{
						eventsSent++;
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
					websocketLogger.LogWarning("Maximum payload size exceeded.");
					await SendClose(4002);
					break;
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

		public Task SendDispatch(GatewayEvent eventInfo) => SendDispatch(eventInfo, WebSocketMessageType.Text, CancellationToken.None);
		public Task SendDispatch(GatewayEvent eventInfo, WebSocketMessageType type) => SendDispatch(eventInfo, type, CancellationToken.None);
		public Task SendDispatch(GatewayEvent eventInfo, WebSocketMessageType type, CancellationToken token)
		{
			var dispatch = new GatewayDispatch(++Sequence, eventInfo);
			events.Add(dispatch);
			return SendEntity(dispatch);
		}

		private async Task HandleClientMessageString(string message)
		{
			try
			{
				var data = JsonConvert.DeserializeObject<GatewayEntity>(message);
				var opcode = data.Opcode;

				switch(opcode)
				{
					case GatewayOpcode.Heartbeat:
					{
						// TODO: validate heartbeat sequence
						await SendEntity(new GatewayEntity(GatewayOpcode.HeartbeatAck));
						break;
					}
					case GatewayOpcode.Identify:
					{
						websocketLogger.LogInformation("Handling Identify");
						GatewayIdentify loginInfo = JsonConvert.DeserializeObject<GatewayIdentify>(message);

						if (string.IsNullOrWhiteSpace(loginInfo.ClientToken))
						{
							websocketLogger.LogWarning("Invalid client token was provided");
							await SendClose(4004);
							return;
						}
						else
						{
							websocketLogger.LogDebug("Client using token {0}", loginInfo.ClientToken);
						}

						var reconnectStatus = service.TryOverwrite(loginInfo.ClientToken, this);
						if (reconnectStatus == ReconnectStatus.AlreadyConnected)
						{
							websocketLogger.LogWarning("A client already exists using the token {0}", loginInfo.ClientToken);
							await SendClose(4005);
							return;
						}
						else if (reconnectStatus == ReconnectStatus.Reconnect)
						{
							websocketLogger.LogInformation("Handling Identify as reconnect: you should use Resume instead or mark a new connection");
						}

						if (loginInfo.ShardInfo != default(int[]))
						{
							// sharding: check two values were provided
							if (loginInfo.ShardInfo.Length != 2)
							{
								websocketLogger.LogWarning("Invalid shard info (expected 2 values, got {0})", loginInfo.ShardInfo.Length);
								await SendClose(4010);
								return;
							}
							else
							{
								var shardId = loginInfo.ShardInfo[0];
								var shardCount = loginInfo.ShardInfo[1];

								if (shardId < 0)
								{
									websocketLogger.LogWarning("Invalid shard info (shardId must be greater than or equal to 0)");
									await SendClose(4010);
									return;
								}
								if (shardCount < 1)
								{
									websocketLogger.LogWarning("Invalid shard info (shardCount must be greater than 0)");
									await SendClose(4010);
									return;
								}

								if (shardId >= shardCount)
								{
									websocketLogger.LogWarning("Invalid shard info (shardId {0} must be less than shardCount {1})", shardId, shardCount);
									await SendClose(4010);
									return;
								}
								else
								{
									websocketLogger.LogDebug("Sharding information provided: id={0} count={1}", shardId, shardCount);
								}
							}
						}
						// if we go this far, the Identify packet was valid.
						await SendIdentifyResponse();
						break;
					}
					case GatewayOpcode.Resume:
					{
						websocketLogger.LogInformation("Handling Resume");
						GatewayResume resumeInfo = JsonConvert.DeserializeObject<GatewayResume>(message);

						bool resumeSuccess = true;
						WebSocketController oldClient = null;

						if (string.IsNullOrWhiteSpace(resumeInfo.ClientToken))
						{
							websocketLogger.LogWarning("Invalid client token was provided");
							resumeSuccess = false;
						}
						else
						{
							websocketLogger.LogDebug("Client using token {0}", resumeInfo.ClientToken);

							service.TryGet(resumeInfo.ClientToken, out oldClient);
							var reconnectStatus = service.TryOverwrite(resumeInfo.ClientToken, this);
							if (reconnectStatus == ReconnectStatus.AlreadyConnected)
							{
								websocketLogger.LogWarning("A client already exists using the token {0}", resumeInfo.ClientToken);
								resumeSuccess = false;
							}
							else if (reconnectStatus == ReconnectStatus.InitialConnect)
							{
								websocketLogger.LogWarning("Client tried to resume a non-existant session");
								resumeSuccess = false;
							}

							if (resumeInfo.SessionId != oldClient.SessionId)
							{
								websocketLogger.LogWarning("Client tried to use an invalid session id");
								resumeSuccess = false;
							}

							if (resumeInfo.SequenceNumber > oldClient.Sequence)
							{
								websocketLogger.LogWarning("Client tried to use an invalid sequence number");
								resumeSuccess = false;
							}
						}

						await SendResumeResponse(resumeInfo, oldClient, resumeSuccess);

						break;
					}
					default:
					{
						websocketLogger.LogWarning("Unknown opcode {0}", opcode);
						await SendClose(4001);
						break;
					}
				}
			}
			catch (JsonException e)
			{
				websocketLogger.LogTrace("Exception occured while deserializing JSON: {0}", e);
				await SendClose(4002);
			}
		}

		private async Task SendIdentifyResponse()
		{
			// TODO:
			//await SendDispatch(new EventReady(SessionId))
		}

		private async Task SendResumeResponse(GatewayResume resumeInfo, WebSocketController previousSession, bool resumeSuccess)
		{
			if (!resumeSuccess)
			{
				await SendEntity(new GatewayEntity(GatewayOpcode.InvalidSession));
			}
			else
			{
				// TODO: replay events
				int sequenceNumber = resumeInfo.SequenceNumber;
				IEnumerable<GatewayDispatch> dispatches = previousSession.events.Where(x => x.Sequence > sequenceNumber && x.Sequence < Sequence);
				foreach (GatewayDispatch dispatch in dispatches)
				{
					await SendEntity(dispatch);
				}
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