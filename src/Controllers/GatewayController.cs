using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Smallscord.Controllers
{
	[Route("api/gateway")]
	public class GatewayController : Controller
	{
		private static readonly Dictionary<string, string> GetGatewayInfo = new Dictionary<string, string>(){
			["url"] = "wss://localhost/"
		};

		public Dictionary<string, string> GetAll()
		{
			return GetGatewayInfo; 
		}
	}
}