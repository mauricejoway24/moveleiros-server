namespace MoveleirosChatServer.Auth
{
    public class JwtEncodeResult
    {
        public bool AnyErrors { get; set; }
        public string Token { get; set; }
        public string ErrorMessage { get; set; }
    }
}