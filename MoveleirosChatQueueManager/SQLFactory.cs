using System.Data.SqlClient;

namespace MoveleirosChatQueueManager
{
    public static class SQLFactory
    {
        public static SqlConnection GetNewConnection()
        {
            return new SqlConnection("Data Source=SRVMSSQL\\MOVELEIROS_HOMOL;Initial Catalog=DbMarketplace2;Integrated Security=False;Persist Security Info=False;User ID=sa;Password=M0vHl85@");
        }
    }
}
