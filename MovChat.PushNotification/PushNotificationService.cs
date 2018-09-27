using MovChat.Core.Hub;
using MovChat.Core.Logger;
using MovChat.Core.Messaging;
using MovChat.Data.Repositories;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MovChat.PushNotification
{
    public class PushNotificationService
    {
#if DEBUG
        public const string BASE_URL = "http://localhost:8081/#";
#else
        public const string BASE_URL = "https://webchat.moveleiros.com.br/#";
#endif
        public async Task SendPushMessages(
            IEnumerable<TokenDetail> agent,
            string channelId,
            LivechatUserTokenRepository livechatTokenRep,
            IMessageHubManager messageHubManager = null,
            LogRepository logMessageRepository = null,
            string defaultDataAction = "JoinNewChannel",
            string defaultNotificationTitle = "Novo cliente!",
            string defaultNotificationBody = "Um cliente quer falar com você, rápido!")
        {
            foreach (var token in agent)
            {
                // For new desktop version, it doesn't include firebase token
                if (token.Device.ToLower() == "desktop" && !string.IsNullOrEmpty(token.Version))
                {
                    if (string.IsNullOrEmpty(token.Token))
                        continue;

                    var package = new Dictionary<string, string>
                    {
                        { "body", "Um cliente quer falar com você, Rápido!" },
                        { "title", "Novo cliente!" },
                        { "icon", "/static/img/launcher-icon-4x.png" },
                        { "priority", "high" },
                        { "channelId", channelId },
                        { "sound", "default" }
                    };

                    await messageHubManager.SendClientMessage(
                        token.Token,
                        HubMessages.NEW_CUSTOMER_ALERT,
                        package);

                    continue;
                }

                using (var client = new WebClient())
                {
                    client.Headers["Authorization"] = "key=AAAAJu4yfbc:APA91bHpQ4yVwm4zJ6Tvv73fdutEM17oJE8WzwRTWTgG9KMRfhEwD1Rd04YVB78pvEbwxC5HYWDh8evTJ1GLBRWtTWRT0S3fdSmljNlPSwprBkbZsx2pdlKFfIRSGf9fPy1CCfIu7nxI";
                    client.Headers["Content-Type"] = "application/json";

                    var clientMobileMessage = $"{{\"title\":\"{defaultNotificationTitle}\",\"text\":\"{defaultNotificationBody}\"," +
                        $"\"attachments\":[],\"data\":{{\"action\": \"{defaultDataAction}\",\"p1\":\"{channelId}\"}}," +
                        $"\"trigger\":{{\"in\":1,\"unit\":\"second\"}},\"foreground\":true,\"autoClear\":true," +
                        $"\"defaults\":0,\"groupSummary\":false,\"id\":0,\"launch\":true,\"led\":true," +
                        $"\"lockscreen\":true,\"number\":0,\"priority\":1,\"showWhen\":true,\"silent\":false," +
                        $"\"smallIcon\":\"res://icon\",\"sound\":true,\"vibrate\":true,\"wakeup\":true}}";


                    var package = new Dictionary<string, object>
                    {
                        { "to", token.Token },
                        { "notification", new Dictionary<string, string>
                            {
                                { "body", "Um cliente quer falar com você, Rápido!" },
                                { "title", "Novo cliente!" },
                                { "icon", "/static/img/launcher-icon-4x.png" },
                                { "priority", "high" },
                                { "click_action", $"FCM_PLUGIN_ACTIVITY" },
                                { "sound", "default" }
                            }
                        },
                        { "data", new Dictionary<string, string>()
                            {
                                { "body", "Um cliente quer falar com você, rápido!" },
                                { "title", "Novo cliente!" },
                                { "click_action", $"{BASE_URL}/newchat?channelId={channelId}" },
                                { "chat_mobile", clientMobileMessage }
                            }
                        }
                    };

                    if (token.Device.ToLower() == "desktop")
                        (package["notification"] as Dictionary<string, string>)["click_action"] = $"{BASE_URL}/newchat?channelId={channelId}";
                    else
                        package.Remove("notification");

                    var jsonPackage = JsonConvert.SerializeObject(package);
                    var response = client.UploadStringTaskAsync("https://fcm.googleapis.com/fcm/send", jsonPackage);
                    /*if (response.Contains("NotRegistered"))
                        await livechatTokenRep.RemoveInvalidToken(token.Token); */

                    if (logMessageRepository != null)
                    {
                        await logMessageRepository.RecLog(
                            NopLogLevel.Info,
                            $"Token sent to {token.Token}",
                            $"Message response: {response} \n\n LivechatUserId: {token.LivechatUserId}"
                        );
                    }
                }
            }
        }

        public class SendPackage
        {
            public string to { get; set; }
            public Dictionary<string, string> notification { get; set; }
            public Dictionary<string, string> data { get; set; }
            public string priority => "high";
        }

        public class NotificationPackage
        {
            public string body { get; set; }
            public string title { get; set; }
            public string sound => "default";
            public string icon { get; set; }
            public string click_action => "FCM_PLUGIN_ACTIVITY";
        }
    }
}
