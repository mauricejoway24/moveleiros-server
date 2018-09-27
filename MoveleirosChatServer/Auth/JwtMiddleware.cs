using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoveleirosChatServer.Auth
{
    public static class JwtMiddleware
    {
        /// <summary>
        /// This middleware is responsable to validade a JWT sent on Authorization header
        /// </summary>
        /// <param name="app"></param>
        public static async Task UseJwtMiddleware(HttpContext ctx, Func<Task> next)
        {
            var request = ctx.Request;

            if (!request.Headers.ContainsKey("Authorization"))
            {
                await next();
                return;
            }

            var bearer = request.Headers["Authorization"];
            var bearerSplit = bearer.ToString().Split(' ', 2);

            if (bearerSplit.Length < 2)
            {
                ctx.Response.StatusCode = 500;
                await next();
                return;
            }

            var bearerToken = bearerSplit[1];

            var tokenDecoded = new JwtMovDecoder()
                .Decode(bearerToken);

            if (tokenDecoded.AnyErrors)
            {
                ctx.Response.StatusCode = 401;
                await next();
                return;
            }

            var payload = tokenDecoded.Payload;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, payload[ClaimTypes.Name]?.ToString() ?? ""),
                new Claim(ClaimTypes.Email, payload[ClaimTypes.Email]?.ToString() ?? ""),
                new Claim(ClaimTypes.Role, payload[ClaimTypes.Role]?.ToString() ?? ""),
                new Claim("CustomerId", payload.ContainsKey("CustomerId") ? payload["CustomerId"].ToString() : "0"),
                new Claim("LivechatUserId", payload["LivechatUserId"].ToString()),
                new Claim("Stores", payload.ContainsKey("Stores") ? payload["Stores"].ToString() : ""),
                new Claim("UserPayload", payload.ContainsKey("UserPayload") ? payload["UserPayload"].ToString() : ""),
                new Claim("AuthToken", bearerToken),
                new Claim("Device", payload.ContainsKey("Device") ? payload["Device"]?.ToString() ?? "" : ""),
                new Claim("Version", payload.ContainsKey("Version") ? payload["Version"]?.ToString() ?? "" : "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);

            ctx.User = new ClaimsPrincipal(claimsIdentity);

            await next();
        }
    }
}
