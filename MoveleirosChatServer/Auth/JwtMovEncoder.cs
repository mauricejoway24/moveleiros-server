using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System.Collections.Generic;

namespace MoveleirosChatServer.Auth
{
    public class JwtMovEncoder
    {
        /// <summary>
        /// Encode a payload into a JWT token
        /// </summary>
        /// <param name="payload">Dictionary containing the payload</param>
        /// <param name="secret">Token secret</param>
        /// <returns>JwtEncodeResult</returns>
        public JwtEncodeResult Encode(Dictionary<string, object> payload, string secret = JwtDefaults.DEFAULT_SECRET)
        {
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtEncoder encoder = new JWT.JwtEncoder(algorithm, serializer, urlEncoder);

            var token = encoder.Encode(payload, secret);

            return new JwtEncodeResult
            {
                AnyErrors = false,
                ErrorMessage = string.Empty,
                Token = token
            };
        }
    }
}
