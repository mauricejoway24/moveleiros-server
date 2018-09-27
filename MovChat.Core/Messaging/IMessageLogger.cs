using MovChat.Core.Models;
using System.Threading.Tasks;

namespace MovChat.Core.Messaging
{
    public interface IMessageLogger
    {
        Task CreateLog(LivechatMessagePack pack);
    }
}
