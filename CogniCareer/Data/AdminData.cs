using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class AdminData
    {
        public AdminStats GetStats()
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_AdminGetDashboardStats", con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new AdminStats
                    {
                        TotalStudents = Convert.ToInt32(reader["TotalStudents"]),
                        TotalCompanies = Convert.ToInt32(reader["TotalCompanies"]),
                        TotalActiveJobs = Convert.ToInt32(reader["TotalActiveJobs"]),
                        TotalApplications = Convert.ToInt32(reader["TotalApplications"]),
                        PendingApprovals = Convert.ToInt32(reader["PendingApprovals"]),
                        AverageMatchScore = Convert.ToDecimal(reader["AverageMatchScore"])
                    };
                }
            }
            catch { }
            return new AdminStats();
        }

        public List<User> GetAllUsers()
        {
            var list = new List<User>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT * FROM Users ORDER BY CreatedAt DESC", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new User
                    {
                        UserID = Convert.ToInt32(reader["UserID"]),
                        FullName = reader["FullName"].ToString() ?? "",
                        Email = reader["Email"].ToString() ?? "",
                        Role = reader["Role"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
                }
            }
            catch { }
            return list;
        }
    }
}