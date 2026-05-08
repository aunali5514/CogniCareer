using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class StudentSkillData
    {
        public List<StudentSkill> GetByUserID(int userID)
        {
            var list = new List<StudentSkill>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetStudentSkills", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new StudentSkill
                    {
                        StudentSkillID = Convert.ToInt32(reader["StudentSkillID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        SkillID = Convert.ToInt32(reader["SkillID"]),
                        SkillName = reader["SkillName"].ToString() ?? "",
                        Category = reader["Category"].ToString() ?? "",
                        ProficiencyLevel = reader["ProficiencyLevel"].ToString() ?? ""
                    });
                }
            }
            catch { }
            return list;
        }

        public bool AddStudentSkill(StudentSkill s)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_AddStudentSkill", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", s.UserID);
                cmd.Parameters.AddWithValue("@SkillID", s.SkillID);
                cmd.Parameters.AddWithValue("@ProficiencyLevel", s.ProficiencyLevel);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool UpdateStudentSkill(int studentSkillID, string proficiency)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_UpdateStudentSkill", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StudentSkillID", studentSkillID);
                cmd.Parameters.AddWithValue("@ProficiencyLevel", proficiency);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool DeleteStudentSkill(int studentSkillID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_DeleteStudentSkill", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StudentSkillID", studentSkillID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool HasSkill(int userID, int skillID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM StudentSkills WHERE UserID=@UserID AND SkillID=@SkillID", con);
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@SkillID", skillID);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch { return false; }
        }
    }
}