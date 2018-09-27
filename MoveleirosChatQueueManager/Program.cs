using System.Threading.Tasks;

namespace MoveleirosChatQueueManager
{
    /// <summary>
    /// This program has 1 objective: Get a queue from persisted in database and save it 
    /// </summary>
    class Program
    {
        public static async Task Main(string[] args)
        {
            var queueDA = new QueueDataAccess();

            // Load queue from database
            // var queue = queueDA.LoadChatAgents();

            // Send notification to all agents

            // 
        }
    }
}
