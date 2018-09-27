using System.Collections.Generic;

namespace MoveleirosChatServer.Auth
{
    public class JwtDecodeResult
    {
        public bool AnyErrors { get; set; }
        public IDictionary<string, object> Payload { get; set; }
        public string ErrorMessage { get; set; }
    }
}
