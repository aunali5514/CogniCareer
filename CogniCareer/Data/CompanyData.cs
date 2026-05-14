using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class CompanyData
    {
        // All SELECT queries now use dbo.vw_Companies which exposes PascalCase aliases:
        //   CompanyID, UserID, CompanyName, Industry, Website, Description, IsApproved, etc.
        //
        // All UPDATE/DELETE queries now target dbo.companies (the real table) using its
        // snake_case columns and the correct PK (id) mapped through the view's CompanyID alias.

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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CompanyData.InsertCompany: {ex.Message}");
                return false;
            }
        }

        public Company? GetByUserID(int userID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                // vw_Companies exposes UserID (aliased from user_id added by the patch)
                using var cmd = new SqlCommand(
                    "SELECT * FROM dbo.vw_Companies WHERE UserID=@UserID", con);
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read()) return MapCompany(reader);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CompanyData.GetByUserID: {ex.Message}");
                return null;
            }
        }

        public List<Company> GetPendingCompanies()
        {
            var list = new List<Company>();
            try
            {
                using var con = DBHelper.GetConnection();
                // vw_Companies computes IsApproved = CASE WHEN status='active' THEN 1 ELSE 0 END
                using var cmd = new SqlCommand(
                    "SELECT * FROM dbo.vw_Companies WHERE IsApproved=0", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(MapCompany(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CompanyData.GetPendingCompanies: {ex.Message}");
            }
            return list;
        }

        public List<Company> GetAllCompanies()
        {
            var list = new List<Company>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("SELECT * FROM dbo.vw_Companies", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(MapCompany(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CompanyData.GetAllCompanies: {ex.Message}");
            }
            return list;
        }

        public bool ApproveCompany(int companyID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                // Real table dbo.companies uses snake_case: status, approved_at, id
                using var cmd = new SqlCommand(
                    "UPDATE dbo.companies SET status='active', approved_at=GETDATE() WHERE id=@id", con);
                cmd.Parameters.AddWithValue("@id", companyID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CompanyData.ApproveCompany: {ex.Message}");
                return false;
            }
        }

        public bool RejectCompany(int companyID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "DELETE FROM dbo.companies WHERE id=@id", con);
                cmd.Parameters.AddWithValue("@id", companyID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CompanyData.RejectCompany: {ex.Message}");
                return false;
            }
        }

        public bool UpdateCompany(Company c)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                // Real table dbo.companies uses snake_case column names
                using var cmd = new SqlCommand(
                    @"UPDATE dbo.companies
                      SET name=@name, industry=@industry, website=@website, description=@description
                      WHERE id=@id", con);
                cmd.Parameters.AddWithValue("@name", c.CompanyName);
                cmd.Parameters.AddWithValue("@industry", c.Industry);
                cmd.Parameters.AddWithValue("@website", c.Website);
                cmd.Parameters.AddWithValue("@description", c.Description);
                cmd.Parameters.AddWithValue("@id", c.CompanyID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CompanyData.UpdateCompany: {ex.Message}");
                return false;
            }
        }

        // ── private helper ────────────────────────────────────────────────────
        private static Company MapCompany(SqlDataReader r) => new Company
        {
            CompanyID = Convert.ToInt32(r["CompanyID"]),
            UserID = r["UserID"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserID"]),
            CompanyName = r["CompanyName"].ToString() ?? "",
            Industry = r["Industry"].ToString() ?? "",
            Website = r["Website"].ToString() ?? "",
            Description = r["Description"].ToString() ?? "",
            IsApproved = Convert.ToBoolean(r["IsApproved"])
        };
    }
}
