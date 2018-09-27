using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Models;
using System.Threading.Tasks;

namespace MovChat.Core.Messaging
{
    public interface IMessageHubManager
    {
        /// <summary>
        /// Returns a current user connection
        /// </summary>
        IClientProxy CurrentUser { get; }

        /// <summary>
        /// It sends a message to a channel which must be a group
        /// </summary>
        /// <param name="messagePack">Message object to be sent</param>
        /// <returns>Async Task</returns>
        Task SendGroupMessage(LivechatMessagePack messagePack);

        /// <summary>
        /// It invokes a client's method.
        /// PS: ALWAYS WITH CURRENT USER
        /// </summary>
        /// <param name="method">Method name to be called</param>
        /// <returns>Async Task</returns>
        Task InvokeAsync(string method);

        /// <summary>
        /// It invokes a client's method.
        /// PS: ALWAYS WITH CURRENT USER 
        /// </summary>
        /// <param name="method">>Method name to be called</param>
        /// <param name="arg1">Object to be send as parameter</param>
        /// <returns>Async Task</returns>
        Task InvokeAsync(string method, object arg1);

        /// <summary>
        /// It invokes a client's method based on a connectionId
        /// </summary>
        /// <param name="connectionId">Client connection id</param>
        /// <param name="method">>Method name to be called</param>
        /// <param name="arg1">Object to be send as parameter</param>
        /// <returns>Async Task</returns>
        Task SendClientMessage(string connectionId, string method, object arg1);
    }
}
