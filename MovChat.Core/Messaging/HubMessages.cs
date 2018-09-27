namespace MovChat.Core.Messaging
{
    public static class HubMessages
    {
        public const string ALREADY_TAKEN = "ALREADY_TAKEN";
        public const string TRYING_FIND_AGENT = "TryingFindAgent";
        public const string AGENT_HAS_ACCEPTED = "AGENT_HAS_ACCEPTED";
        public const string SEND = "Send";
        public const string UPDATE_CHANNEL_LIST = "UPDATE_CHANNEL_LIST";
        public const string NEW_CHANNEL_REGISTERED = "NewChannelRegistry";
        public const string ACTIVE_CHANNELS = "ActiveChannels";
        public const string NO_AGENT_AVAILABLE = "ThereIsNoAgent";
        public const string GET_CUSTOMER_PROFILE = "GetCustomerProfile";
        public const string CHAT_HAS_ENDED = "CHAT_HAS_ENDED";
        public const string INVALID_CHANNEL = "INVALID_CHANNEL";
        public const string FORWARD_URL = "FORWARD_URL";
        public const string CUSTOMER_INTEGRATION_SENT = "CUSTOMER_INTEGRATION_SENT";
        public const string EDIT_CUSTOMER_PROFILE = "EditCustomerProfile";
        public const string EDIT_CUSTOMER_PROFILE_SAVED = "EDIT_CUSTOMER_PROFILE_SAVED";
        public const string QUESTION_FORM_SENT = "QUESTION_FORM_SENT";
        public const string NEW_TOKEN_REGISTERED = "NEW_TOKEN_REGISTERED";
        public const string USER_IS_TYPING = "USER_IS_TYPING";
        public const string USER_IS_NOT_TYPING = "USER_IS_NOT_TYPING";
        public const string NEW_CUSTOMER_ALERT = "NEW_CUSTOMER_ALERT";
        public const string NEW_GROUP_MESSAGE = "NEW_GROUP_MESSAGE";
        public const string LAST_TRYING_FIND_AGENT = "LastTryingFindAgent";
        public const string END_CHAT_MESSAGE = "END_CHAT_MESSAGE";
        public const string TYPING_START = "TYPING_START";
    }
}
