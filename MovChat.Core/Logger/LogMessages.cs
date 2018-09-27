namespace MovChat.Core.Logger
{
    public class LogMessages
    {
        public const string ON_CONNECTED_NO_RULES = "OnConnectedAsync generates a call with no rules inside.currentUserDetails.Role has no match.";
        public const string NO_AGENTS_AVAILABLE = "No agents available to store {0}";
        public const string PUSH_SENT_TO_STORE = "Push notification sent to store {0}";
        public const string AGENT_HAS_ACCEPT_CHAT = "Agent {0} has accepted a chat connection.";
        public const string BOT_HAS_ACCEPTED = "Bot has accepted on store {0}";
        public const string STORE_CREDENTIALS_NOT_FOUND = "There's no store credentials configured to use a bot in store {0}";

        /// <summary>
        /// It gives you a message accordingly to its parameters.
        /// It executes a string.Format(**) inside
        /// </summary>
        /// <param name="format">String format</param>
        /// <param name="arguments">Arguments to be format</param>
        /// <returns></returns>
        public static string Fmt(string format, params object[] arguments)
        {
            return string.Format(format, arguments);
        }
    }
}
