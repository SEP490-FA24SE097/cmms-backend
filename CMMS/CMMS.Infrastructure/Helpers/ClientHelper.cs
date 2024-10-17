using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Helpers
{
	public class ClientHelper
	{
		public static string GetIpAddress(HttpContext context)
		{
			string ipAddress = context.Connection.RemoteIpAddress?.ToString();
			if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
			{
				ipAddress = context.Request.Headers["X-Forwarded-For"].ToString();
			}
			return ipAddress;
		}
	}
}
