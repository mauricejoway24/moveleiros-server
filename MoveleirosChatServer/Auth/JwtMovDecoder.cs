using JWT;
using JWT.Serializers;
using System.Collections.Generic;

namespace MoveleirosChatServer.Auth
{
    public class JwtMovDecoder
    {
        /// <summary>
        /// Decode a JWT token 
        /// </summary>
        /// <param name="token">JWT token encoded</param>
        /// <param name="secret">Specify a secret for the token</param>
        /// <param name="verify">Should decode verify token integrady before decrypt it</param>
        /// <returns>JWTDecodeResult</returns>
        public JwtDecodeResult Decode(string token, string secret = JwtDefaults.DEFAULT_SECRET, bool verify = true)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || token.ToLower() == "null")
                {
                    return new JwtDecodeResult
                    {
                        AnyErrors = true,
                        Payload = null,
                        ErrorMessage = "Token is not valid"
                    };
                }
                IJsonSerializer serializer = new JsonNetSerializer();
                IDateTimeProvider provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtDecoder decoder = new JWT.JwtDecoder(serializer, validator, urlEncoder);

                var payload = decoder.DecodeToObject<IDictionary<string, object>>(token, secret, verify: true);

                return new JwtDecodeResult
                {
                    AnyErrors = false,
                    Payload = payload,
                    ErrorMessage = string.Empty
                };
            }
            catch (TokenExpiredException)
            {
                return new JwtDecodeResult
                {
                    AnyErrors = true,
                    Payload = null,
                    ErrorMessage = JwtDefaults.TOKEN_EXPIRED
                };
            }
            catch (SignatureVerificationException)
            {
                return new JwtDecodeResult
                {
                    AnyErrors = true,
                    Payload = null,
                    ErrorMessage = JwtDefaults.INVALID_SIGNATURE
                };
            }
        }
    }
}
