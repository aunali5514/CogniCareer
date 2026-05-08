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
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT j.*, c.CompanyName FROM Jobs j JOIN Companies c ON j.CompanyID=c.CompanyID WHERE j.JobID=@JobID", con);
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read()) return MapJob(reader);
                return null;
            }
            catch { return null; }
        }

        public bool CloseJob(int jobID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE Jobs SET Status='Closed' WHERE JobID=@JobID", con);
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
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

        private Job MapJob(SqlDataReader r)
        {
            return new Job
            {
                JobID = Convert.ToInt32(r["JobID"]),
                CompanyID = Convert.ToInt32(r["CompanyID"]),
                CompanyName = r.HasColumn("CompanyName") ? r["CompanyName"].ToString() ?? "" : "",
                Title = r["Title"].ToString() ?? "",
                Description = r["Description"].ToString() ?? "",
                JobType = r["JobType"].ToString() ?? "",
                Duration = r["Duration"].ToString() ?? "",
                Deadline = Convert.ToDateTime(r["Deadline"]),
                Status = r["Status"].ToString() ?? "",
                PostedAt = Convert.ToDateTime(r["PostedAt"])
            };
        }
    }
}