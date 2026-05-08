using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class UserData
    {
        public int RegisterUser(User user)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_RegisterUser", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FullName", user.FullName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        public User? GetByEmail(string email)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetUserByEmail", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", email);
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        UserID = Convert.ToInt32(reader["UserID"]),
                        FullName = reader["FullName"].ToString() ?? "",
                        Email = reader["Email"].ToString() ?? "",
                        PasswordHash = reader["PasswordHash"].ToString() ?? "",
                        Role = reader["Role"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    };
                }
                return null;
            }
            catch { return null; }
        }

        public User? GetByID(int userID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetUserByID", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        UserID = Convert.ToInt32(reader["UserID"]),
                        FullName = reader["FullName"].ToString() ?? "",
                        Email = reader["Email"].ToString() ?? "",
                        PasswordHash = reader["PasswordHash"].ToString() ?? "",
                        Role = reader["Role"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    };
                }
                return null;
            }
            catch { return null; }
        }

        public bool EmailExists(string email)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE Email=@Email", con);
                cmd.Parameters.AddWithValue("@Email", email);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch { return false; }
        }

        public bool UpdateStatus(int userID, bool isActive)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_DeactivateUser", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
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