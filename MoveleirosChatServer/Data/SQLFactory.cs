using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace MoveleirosChatServer.Data
{
    public class SQLFactory
    {
        private IConfiguration configuration;

        public SQLFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public SqlConnection GetNewConnection()
        {
            return new SqlConnection(configuration["ConnectionString"]);
        }
    }
}
