using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class StudentData
    {
        public bool InsertProfile(StudentProfile p)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_InsertStudentProfile", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", p.UserID);
                cmd.Parameters.AddWithValue("@University", p.University);
                cmd.Parameters.AddWithValue("@Degree", p.Degree);
                cmd.Parameters.AddWithValue("@Semester", p.Semester);
                cmd.Parameters.AddWithValue("@GPA", p.GPA);
                cmd.Parameters.AddWithValue("@ExpectedGradYear", p.ExpectedGradYear);
                cmd.Parameters.AddWithValue("@IsProfileComplete", p.IsProfileComplete);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public StudentProfile? GetProfile(int userID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetStudentProfile", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new StudentProfile
                    {
                        ProfileID = Convert.ToInt32(reader["ProfileID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        University = reader["University"].ToString() ?? "",
                        Degree = reader["Degree"].ToString() ?? "",
                        Semester = Convert.ToInt32(reader["Semester"]),
                        GPA = Convert.ToDecimal(reader["GPA"]),
                        ExpectedGradYear = Convert.ToInt32(reader["ExpectedGradYear"]),
                        IsProfileComplete = Convert.ToBoolean(reader["IsProfileComplete"])
                    };
                }
                return null;
            }
            catch { return null; }
        }

        public bool UpdateProfile(StudentProfile p)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_UpdateStudentProfile", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", p.UserID);
                cmd.Parameters.AddWithValue("@University", p.University);
                cmd.Parameters.AddWithValue("@Degree", p.Degree);
                cmd.Parameters.AddWithValue("@Semester", p.Semester);
                cmd.Parameters.AddWithValue("@GPA", p.GPA);
                cmd.Parameters.AddWithValue("@ExpectedGradYear", p.ExpectedGradYear);
                cmd.Parameters.AddWithValue("@IsProfileComplete", p.IsProfileComplete);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool ProfileExists(int userID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM StudentProfiles WHERE UserID=@UserID", con);
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch { return false; }
        }
    }
}