using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class UserData
    {
        public int RegisterUser(User user, object result)
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
                var result2 = cmd.ExecuteScalar();
                return result2 != null ? Convert.ToInt32(result2) : 0;
            }
            catch (SqlException ex) when (ex.Number == 208) // Invalid object name (stored procedure missing)
            {
                // Fallback: perform direct INSERT into Users table
                using var con = DBHelper.GetConnection();
                var insertCmd = new SqlCommand(@"INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, IsActive, CreatedAt) 
                                            VALUES (@FullName, @Email, @PasswordHash, @Role, 1, GETDATE()); 
                                            SELECT SCOPE_IDENTITY();", con);
                insertCmd.Parameters.AddWithValue("@FullName", user.FullName);
                insertCmd.Parameters.AddWithValue("@Email", user.Email);
                insertCmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                insertCmd.Parameters.AddWithValue("@Role", user.Role);
                con.Open();
                var v = insertCmd.ExecuteScalar();
                var insertResult = v;
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
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
                        PasswordHash = reader["PasswordHash"].ToString()?.Trim() ?? "",
                        Role = reader["Role"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    };
                }
                return null;
            }
            catch (SqlException ex) when (ex.Number == 208) // missing proc
            {
                // Fallback: direct query on sqlUsers table
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("SELECT TOP 1 * FROM dbo.Users WHERE Email = @Email", con);
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
                        PasswordHash = reader["PasswordHash"].ToString()?.Trim() ?? "",
                        Role = reader["Role"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
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
                    "SELECT COUNT(*) FROM dbo.Users WHERE Email=@Email", con);
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
                con.Open();
                cmd.ExecuteNonQuery();
                // If activation flag is true, we need to reactivate via direct SQL as stored proc only deactivates
                if (isActive)
                {
                    using var reactCmd = new SqlCommand("UPDATE dbo.Users SET IsActive = 1 WHERE UserID = @UserID", con);
                    reactCmd.Parameters.AddWithValue("@UserID", userID);
                    reactCmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (SqlException ex) when (ex.Number == 208) // missing proc
            {
                // Fallback: direct update
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("UPDATE dbo.Users SET IsActive = @IsActive WHERE UserID = @UserID", con);
                cmd.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<User> GetAllUsers()
        {
            var list = new List<User>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT * FROM dbo.Users ORDER BY CreatedAt DESC", con);
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