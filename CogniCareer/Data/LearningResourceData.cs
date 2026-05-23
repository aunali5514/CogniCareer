using Microsoft.Data.SqlClient;
using CogniCareer.Models;

namespace CogniCareer.Data
{
    public class LearningResourceData
    {
        public List<LearningResource> GetBySkillIDs(List<int> skillIDs)
        {
            var list = new List<LearningResource>();
            if (skillIDs == null || !skillIDs.Any()) return list;
            try
            {
                var paramNames = skillIDs.Select((id, i) => "@sid" + i).ToList();
                var inClause = string.Join(",", paramNames);
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    $"SELECT id, skill_id, title, provider, url, resource_type, is_free " +
                    $"FROM dbo.learning_resources WHERE skill_id IN ({inClause}) " +
                    $"ORDER BY skill_id, id", con);
                for (int i = 0; i < skillIDs.Count; i++)
                    cmd.Parameters.AddWithValue(paramNames[i], skillIDs[i]);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new LearningResource
                    {
                        ResourceID = Convert.ToInt32(reader["id"]),
                        SkillID = Convert.ToInt32(reader["skill_id"]),
                        Title = reader["title"]?.ToString() ?? "",
                        Provider = reader["provider"]?.ToString() ?? "",
                        URL = reader["url"]?.ToString() ?? "",
                        ResourceType = reader["resource_type"]?.ToString() ?? "",
                        IsFree = reader["is_free"] != DBNull.Value && Convert.ToBoolean(reader["is_free"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] LearningResourceData.GetBySkillIDs: {ex.Message}");
            }
            return list;
        }
    }
}