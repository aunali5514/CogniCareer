using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class AlertData
    {
        public bool InsertAlert(Alert a)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_InsertAlert", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", a.UserID);
                cmd.Parameters.AddWithValue("@Message", a.Message);
                cmd.Parameters.AddWithValue("@AlertType", a.AlertType);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] {GetType().Name}: {ex.Message}"); return false; }
        }

        public List<Alert> GetUnread(int userID)
        {
            var list = new List<Alert>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetUnreadAlerts", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Alert
                    {
                        AlertID = Convert.ToInt32(reader["AlertID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        Message = reader["Message"].ToString() ?? "",
                        AlertType = reader["AlertType"].ToString() ?? "",
                        IsRead = Convert.ToBoolean(reader["IsRead"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {GetType().Name}: {ex.Message}");
                return null; // keep whatever return was there originally
            }
            return list;
        }

        public bool MarkRead(int alertID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_MarkAlertRead", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@AlertID", alertID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] {GetType().Name}: {ex.Message}"); return false; }
        }

        public bool MarkAllRead(int userID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_MarkAllAlertsRead", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] {GetType().Name}: {ex.Message}"); return false; }
        }
    }
}