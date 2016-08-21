using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smallscord.WebSocketControllers;

namespace Smallscord
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();
			services.AddSingleton<WebSocketController>(x => WebSocketController.GetInstance());
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			var webSocketLogger = loggerFactory.CreateLogger("WebSocket");

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				ExceptionHandlerOptions exceptionOptions = new ExceptionHandlerOptions();
				exceptionOptions.ExceptionHandler = async context =>
				{
					context.Response.StatusCode = 500;
					context.Response.ContentType = "application/json";
					await context.Response.WriteAsync("{}"); // TODO: handle this
				};
				app.UseExceptionHandler(exceptionOptions);
			}

			// Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715
			//WebSocketOptions options = new WebSocketOptions();
			//options.KeepAliveInterval = TimeSpan.FromMilliseconds(0);
			app.UseWebSockets();//options);

			app.Use(async (context, next) => 
			{
				webSocketLogger.LogDebug("Client connecting");
				if (context.WebSockets.IsWebSocketRequest)
				{
					webSocketLogger.LogDebug("Was websocket");
					var websocket = await context.WebSockets.AcceptWebSocketAsync();
					await new WebSocketController(websocket, loggerFactory).Run();
				}
				else
				{
					webSocketLogger.LogDebug("Not websocket");
					await next();
				}
			});

			app.UseMvc();
		}
	}
}
