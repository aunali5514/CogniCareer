using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class StatusLogData
    {
        public bool InsertLog(ApplicationStatusLog log)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_InsertApplicationStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApplicationID", log.ApplicationID);
                cmd.Parameters.AddWithValue("@Status", log.Status);
                cmd.Parameters.AddWithValue("@ChangedByUserID", log.ChangedByUserID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public List<ApplicationStatusLog> GetByApplication(int applicationID)
        {
            var list = new List<ApplicationStatusLog>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetApplicationHistory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApplicationID", applicationID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ApplicationStatusLog
                    {
                        LogID = Convert.ToInt32(reader["LogID"]),
                        ApplicationID = Convert.ToInt32(reader["ApplicationID"]),
                        Status = reader["Status"].ToString() ?? "",
                        ChangedAt = Convert.ToDateTime(reader["ChangedAt"])
                    });
                }
            }
            catch { }
            return list;
        }
    }
}
