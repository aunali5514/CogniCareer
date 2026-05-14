using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class SkillData
    {
        // dbo.skills columns: id, name, category, icon, demand_pct, jobs_count, created_at
        // There is no IsActive column — all rows in dbo.skills are considered active.
        // SkillID  = id
        // SkillName = name

        public List<Skill> GetAllSkills()
        {
            var list = new List<Skill>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT id, name, category FROM dbo.skills ORDER BY category, name", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Skill
                    {
                        SkillID = Convert.ToInt32(reader["id"]),
                        SkillName = reader["name"].ToString() ?? "",
                        Category = reader["category"].ToString() ?? "",
                        IsActive = true   // no IsActive column; every row is active
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SkillData.GetAllSkills: {ex.Message}");
            }
            return list;
        }

        public List<Skill> GetActiveSkills()
        {
            // No IsActive column exists — all skills in the table are active by definition.
            // Delegates to GetAllSkills() so there is one place to maintain the query.
            return GetAllSkills();
        }

        public bool AddSkill(Skill s)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "INSERT INTO dbo.skills (name, category) VALUES (@name, @category)", con);
                cmd.Parameters.AddWithValue("@name", s.SkillName);
                cmd.Parameters.AddWithValue("@category", s.Category);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SkillData.AddSkill: {ex.Message}");
                return false;
            }
        }

        public bool UpdateSkill(Skill s)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE dbo.skills SET name=@name, category=@category WHERE id=@id", con);
                cmd.Parameters.AddWithValue("@name", s.SkillName);
                cmd.Parameters.AddWithValue("@category", s.Category);
                cmd.Parameters.AddWithValue("@id", s.SkillID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SkillData.UpdateSkill: {ex.Message}");
                return false;
            }
        }

        public bool DeleteSkill(int skillID)
        {
            // dbo.skills has no IsActive flag — delete the row outright.
            // If you want soft-delete, add an is_active column to dbo.skills first.
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "DELETE FROM dbo.skills WHERE id=@id", con);
                cmd.Parameters.AddWithValue("@id", skillID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SkillData.DeleteSkill: {ex.Message}");
                return false;
            }
        }
    }
}
