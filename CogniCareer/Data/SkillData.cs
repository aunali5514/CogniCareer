using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class SkillData
    {
        public List<Skill> GetAllSkills()
        {
            var list = new List<Skill>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT * FROM Skills ORDER BY Category, SkillName", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Skill
                    {
                        SkillID = Convert.ToInt32(reader["SkillID"]),
                        SkillName = reader["SkillName"].ToString() ?? "",
                        Category = reader["Category"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(reader["IsActive"])
                    });
                }
            }
            catch { }
            return list;
        }

        public List<Skill> GetActiveSkills()
        {
            var list = new List<Skill>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT * FROM Skills WHERE IsActive=1 ORDER BY Category, SkillName", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Skill
                    {
                        SkillID = Convert.ToInt32(reader["SkillID"]),
                        SkillName = reader["SkillName"].ToString() ?? "",
                        Category = reader["Category"].ToString() ?? "",
                        IsActive = true
                    });
                }
            }
            catch { }
            return list;
        }

        public bool AddSkill(Skill s)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "INSERT INTO Skills (SkillName, Category, IsActive) VALUES (@SkillName, @Category, 1)", con);
                cmd.Parameters.AddWithValue("@SkillName", s.SkillName);
                cmd.Parameters.AddWithValue("@Category", s.Category);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool UpdateSkill(Skill s)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE Skills SET SkillName=@SkillName, Category=@Category WHERE SkillID=@SkillID", con);
                cmd.Parameters.AddWithValue("@SkillName", s.SkillName);
                cmd.Parameters.AddWithValue("@Category", s.Category);
                cmd.Parameters.AddWithValue("@SkillID", s.SkillID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool DeleteSkill(int skillID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE Skills SET IsActive=0 WHERE SkillID=@SkillID", con);
                cmd.Parameters.AddWithValue("@SkillID", skillID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }
    }
}