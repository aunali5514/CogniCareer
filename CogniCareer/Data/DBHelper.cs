using Microsoft.Data.SqlClient;

namespace CogniCareer.Data
{
    public static class DBHelper
    {
        public static string ConnectionString { get; set; } = "";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}