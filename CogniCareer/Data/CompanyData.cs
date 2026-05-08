using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class CompanyData
    {
        public bool InsertCompany(Company c)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_InsertCompany", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", c.UserID);
                cmd.Parameters.AddWithValue("@CompanyName", c.CompanyName);
                cmd.Parameters.AddWithValue("@Industry", c.Industry);
                cmd.Parameters.AddWithValue("@Website", c.Website);
                cmd.Parameters.AddWithValue("@Description", c.Description);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public Company? GetByUserID(int userID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT * FROM Companies WHERE UserID=@UserID", con);
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Company
                    {
                        CompanyID = Convert.ToInt32(reader["CompanyID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        CompanyName = reader["CompanyName"].ToString() ?? "",
                        Industry = reader["Industry"].ToString() ?? "",
                        Website = reader["Website"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        IsApproved = Convert.ToBoolean(reader["IsApproved"])
                    };
                }
                return null;
            }
            catch { return null; }
        }

        public List<Company> GetPendingCompanies()
        {
            var list = new List<Company>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT * FROM Companies WHERE IsApproved=0", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Company
                    {
                        CompanyID = Convert.ToInt32(reader["CompanyID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        CompanyName = reader["CompanyName"].ToString() ?? "",
                        Industry = reader["Industry"].ToString() ?? "",
                        Website = reader["Website"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        IsApproved = false
                    });
                }
            }
            catch { }
            return list;
        }

        public List<Company> GetAllCompanies()
        {
            var list = new List<Company>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("SELECT * FROM Companies", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Company
                    {
                        CompanyID = Convert.ToInt32(reader["CompanyID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        CompanyName = reader["CompanyName"].ToString() ?? "",
                        Industry = reader["Industry"].ToString() ?? "",
                        IsApproved = Convert.ToBoolean(reader["IsApproved"])
                    });
                }
            }
            catch { }
            return list;
        }

        public bool ApproveCompany(int companyID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE Companies SET IsApproved=1, ApprovedAt=GETDATE() WHERE CompanyID=@CompanyID", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool RejectCompany(int companyID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "DELETE FROM Companies WHERE CompanyID=@CompanyID", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool UpdateCompany(Company c)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE Companies SET CompanyName=@CompanyName, Industry=@Industry, Website=@Website, Description=@Description WHERE CompanyID=@CompanyID", con);
                cmd.Parameters.AddWithValue("@CompanyName", c.CompanyName);
                cmd.Parameters.AddWithValue("@Industry", c.Industry);
                cmd.Parameters.AddWithValue("@Website", c.Website);
                cmd.Parameters.AddWithValue("@Description", c.Description);
                cmd.Parameters.AddWithValue("@CompanyID", c.CompanyID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }
    }
}