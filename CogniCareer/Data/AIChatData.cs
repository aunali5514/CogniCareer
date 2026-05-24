using Microsoft.Data.SqlClient;
using CogniCareer.Models;

namespace CogniCareer.Data
{
    public class AIChatData
    {
        private static bool _schemaEnsured;

        public void EnsureSchema()
        {
            if (_schemaEnsured) return;
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ai_chat_messages' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ai_chat_messages (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ai_chat_messages PRIMARY KEY,
        user_id INT NOT NULL,
        role NVARCHAR(10) NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_ai_chat_messages_created_at DEFAULT (SYSUTCDATETIME())
    );
    CREATE NONCLUSTERED INDEX IX_ai_chat_messages_user_created
        ON dbo.ai_chat_messages (user_id, created_at);
END", con);
                con.Open();
                cmd.ExecuteNonQuery();
                _schemaEnsured = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AIChatData.EnsureSchema: {ex.Message}");
            }
        }

        public void Add(int userId, string role, string content)
        {
            EnsureSchema();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    @"INSERT INTO dbo.ai_chat_messages (user_id, role, content, created_at)
                      VALUES (@uid, @role, @content, SYSUTCDATETIME())", con);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@role", role);
                cmd.Parameters.AddWithValue("@content", content);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AIChatData.Add: {ex.Message}");
            }
        }

        public List<ChatMessage> GetForUser(int userId, int limit = 50)
        {
            EnsureSchema();
            var list = new List<ChatMessage>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    @"SELECT role, content FROM (
                        SELECT TOP (@limit) role, content, created_at
                        FROM dbo.ai_chat_messages
                        WHERE user_id = @uid
                        ORDER BY created_at DESC
                      ) recent
                      ORDER BY created_at ASC", con);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@limit", limit);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ChatMessage
                    {
                        Role = reader["role"].ToString() ?? "user",
                        Content = reader["content"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AIChatData.GetForUser: {ex.Message}");
            }
            return list;
        }

        public void ClearForUser(int userId)
        {
            EnsureSchema();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "DELETE FROM dbo.ai_chat_messages WHERE user_id = @uid", con);
                cmd.Parameters.AddWithValue("@uid", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AIChatData.ClearForUser: {ex.Message}");
            }
        }
    }
}