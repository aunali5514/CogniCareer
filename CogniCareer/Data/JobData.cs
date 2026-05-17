using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class JobData
    {
        public int InsertJob(Job j)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_InsertJob", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CompanyID", j.CompanyID);
                cmd.Parameters.AddWithValue("@Title", j.Title);
                cmd.Parameters.AddWithValue("@Description", j.Description);
                cmd.Parameters.AddWithValue("@JobType", j.JobType);
                cmd.Parameters.AddWithValue("@Duration", j.Duration);
                cmd.Parameters.AddWithValue("@Deadline", j.Deadline);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        public List<Job> GetByCompany(int companyID)
        {
            var list = new List<Job>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetJobsByCompany", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CompanyID", companyID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(MapJob(reader));
            }
            catch { }
            return list;
        }

        public List<Job> GetAllActiveJobs()
        {
            var list = new List<Job>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetAllActiveJobs", con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(MapJob(reader));
            }
            catch { }
            return list;
        }

        public Job? GetByID(int jobID)
        {
            string[] queries =
            {
                @"SELECT j.*, c.CompanyName FROM dbo.jobs j
                  INNER JOIN dbo.vw_Companies c ON j.company_id = c.CompanyID WHERE j.id = @JobID",
                @"SELECT j.*, c.CompanyName FROM Jobs j
                  INNER JOIN Companies c ON j.CompanyID = c.CompanyID WHERE j.JobID = @JobID"
            };
            foreach (var sql in queries)
            {
                try
                {
                    using var con = DBHelper.GetConnection();
                    using var cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@JobID", jobID);
                    con.Open();
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read()) return MapJob(reader);
                }
                catch { }
            }
            return null;
        }

        public bool CloseJob(int jobID)
        {
            if (jobID <= 0) return false;

            string[] updates =
            {
                "UPDATE dbo.jobs SET status='closed' WHERE id=@JobID",
                "UPDATE dbo.Jobs SET Status='Closed' WHERE JobID=@JobID",
                "UPDATE Jobs SET Status='Closed' WHERE JobID=@JobID"
            };

            foreach (var sql in updates)
            {
                try
                {
                    using var con = DBHelper.GetConnection();
                    using var cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@JobID", jobID);
                    con.Open();
                    if (cmd.ExecuteNonQuery() > 0) return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] JobData.CloseJob attempt failed ({sql}): {ex.Message}");
                }
            }

            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_CloseJob", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] JobData.CloseJob: {ex.Message}");
                return false;
            }
        }

        public List<Job> GetAllJobs()
        {
            var list = new List<Job>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT j.*, c.CompanyName FROM Jobs j JOIN Companies c ON j.CompanyID=c.CompanyID ORDER BY j.PostedAt DESC", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(MapJob(reader));
            }
            catch { }
            return list;
        }

        private static string NormalizeJobStatus(string? status)
        {
            var s = status?.Trim() ?? "";
            if (string.Equals(s, "Closed", StringComparison.OrdinalIgnoreCase))
                return "Closed";
            if (string.IsNullOrEmpty(s) || string.Equals(s, "Open", StringComparison.OrdinalIgnoreCase))
                return "Active";
            return s;
        }

        private static int ReadInt(SqlDataReader r, params string[] names)
        {
            foreach (var name in names)
            {
                if (r.HasColumn(name) && r[name] != DBNull.Value)
                    return Convert.ToInt32(r[name]);
            }
            return 0;
        }

        private static string ReadString(SqlDataReader r, params string[] names)
        {
            foreach (var name in names)
            {
                if (r.HasColumn(name) && r[name] != DBNull.Value)
                    return r[name].ToString() ?? "";
            }
            return "";
        }

        private static DateTime ReadDate(SqlDataReader r, params string[] names)
        {
            foreach (var name in names)
            {
                if (r.HasColumn(name) && r[name] != DBNull.Value)
                    return Convert.ToDateTime(r[name]);
            }
            return DateTime.MinValue;
        }

        private Job MapJob(SqlDataReader r)
        {
            return new Job
            {
                JobID = ReadInt(r, "JobID", "id"),
                CompanyID = ReadInt(r, "CompanyID", "company_id"),
                CompanyName = ReadString(r, "CompanyName", "company_name"),
                Title = ReadString(r, "Title", "title"),
                Description = ReadString(r, "Description", "description"),
                JobType = ReadString(r, "JobType", "job_type", "jobtype"),
                Duration = ReadString(r, "Duration", "duration"),
                Deadline = ReadDate(r, "Deadline", "deadline"),
                Status = NormalizeJobStatus(ReadString(r, "Status", "status")),
                PostedAt = ReadDate(r, "PostedAt", "posted_at", "postedat")
            };
        }
    }
}