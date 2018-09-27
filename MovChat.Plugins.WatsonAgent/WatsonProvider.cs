using IBM.WatsonDeveloperCloud.Conversation.v1;
using IBM.WatsonDeveloperCloud.Conversation.v1.Model;
using MovChat.Core.Logger;
using MovChat.Core.UI;
using MovChat.Data.Cache;
using MovChat.Data.Repositories;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MovChat.Plugins.WatsonAgent
{
    public class WatsonProvider
    {
        private readonly UOW uow;
        private readonly int storeId;

        public WatsonProvider(
            UOW uow, 
            int storeId)
        {
            this.uow = uow;
            this.storeId = storeId;
        }

        public async Task<MessageResponse> SendMessageAsync(string message, string lastContext = "")
        {
            var storeCredentials = await StoreCredentialsCache.GetStoreAsync(uow, storeId);

            if (storeCredentials == null)
            {
                var logRep = uow.GetRepository<LogRepository>();
                await logRep.RecLog(
                    NopLogLevel.Error,
                    LogMessages.Fmt(LogMessages.STORE_CREDENTIALS_NOT_FOUND, storeId)
                );

                return null;
            }

            var conversation = new ConversationService(
                storeCredentials.Username, 
                storeCredentials.Password, 
                ConversationService.CONVERSATION_VERSION_DATE_2017_05_26
            );

            var response = conversation.Message(storeCredentials.WorkspaceId, new MessageRequest
            {
                Input = new { text = message },
                Context = string.IsNullOrEmpty(lastContext) ? null : JsonConvert.DeserializeObject(lastContext)
            });

            response.Context = JsonConvert.SerializeObject(response.Context);

            return response;
        }

        public async Task<ParsedMessage> ParseMessage(int storeId, string message)
        {
            var messageSplit = Regex.Split(message, @"<%(.*)%>");
            var result = new ParsedMessage();

            // Removing empty spaces
            messageSplit = messageSplit
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            if (messageSplit.Length > 1)
            {
                var elements = messageSplit[1].Trim();
                // Ex: Buttons.Entity
                var elementMessageParse = elements.Split('.');
                var elementType = elementMessageParse[0];
                var entityKey = elementMessageParse[1];

                switch (elementType.ToLower())
                {
                    case "buttons":
                        result.Elements = await ParseButtons(storeId, entityKey);
                        break;
                    case "action":

                        break;
                    default:
                        break;
                }
            }

            result.Message = messageSplit[0];

            return await Task.FromResult(result);
        }

        private async Task<List<ElementUI>> ParseButtons(int storeId, string entityId)
        {
            var entityValues = await StoreCredentialsCache
                .GetEntityValuesAsync(uow, entityId, storeId);

            var result = new List<ElementUI>();

            foreach (var button in entityValues)
            {
                result.Add(new ButtonUI
                {
                    Text = button.EntityText,
                    Value = button.EntityValue
                });
            }

            return result;
        }
    }
}
