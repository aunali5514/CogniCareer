using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class ApplicationData
    {
        public int InsertApplication(Application a)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_InsertApplication", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobID", a.JobID);
                cmd.Parameters.AddWithValue("@UserID", a.UserID);
                cmd.Parameters.AddWithValue("@MatchScore", a.MatchScore);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        public List<Application> GetByUser(int userID)
        {
            var list = new List<Application>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetApplicationsByUser", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Application
                    {
                        ApplicationID = Convert.ToInt32(reader["ApplicationID"]),
                        JobID = Convert.ToInt32(reader["JobID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        MatchScore = Convert.ToDecimal(reader["MatchScore"]),
                        AppliedAt = Convert.ToDateTime(reader["AppliedAt"]),
                        CurrentStatus = reader["CurrentStatus"].ToString() ?? "",
                        JobTitle = reader["JobTitle"].ToString() ?? "",
                        CompanyName = reader["CompanyName"].ToString() ?? ""
                    });
                }
            }
            catch { }
            return list;
        }

        public List<Application> GetByJob(int jobID)
        {
            var list = new List<Application>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetApplicationsByJob", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Application
                    {
                        ApplicationID = Convert.ToInt32(reader["ApplicationID"]),
                        JobID = Convert.ToInt32(reader["JobID"]),
                        UserID = Convert.ToInt32(reader["UserID"]),
                        MatchScore = Convert.ToDecimal(reader["MatchScore"]),
                        AppliedAt = Convert.ToDateTime(reader["AppliedAt"]),
                        CurrentStatus = reader["CurrentStatus"].ToString() ?? "",
                        JobTitle = reader["JobTitle"].ToString() ?? "",
                        CompanyName = reader["CompanyName"].ToString() ?? ""
                    });
                }
            }
            catch { }
            return list;
        }

        public bool ApplicationExists(int userID, int jobID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Applications WHERE UserID=@UserID AND JobID=@JobID", con);
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch { return false; }
        }

        public bool UpdateMatchScore(int applicationID, decimal score)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE Applications SET MatchScore=@Score WHERE ApplicationID=@ApplicationID", con);
                cmd.Parameters.AddWithValue("@Score", score);
                cmd.Parameters.AddWithValue("@ApplicationID", applicationID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public PeerBenchmark GetPeerBenchmark(int userID, int jobID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_GetPeerBenchmark", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@JobID", jobID);
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new PeerBenchmark
                    {
                        TotalApplicants = Convert.ToInt32(reader["TotalApplicants"]),
                        TopScore = Convert.ToDecimal(reader["TopScore"]),
                        MyScore = Convert.ToDecimal(reader["MyScore"]),
                        Percentile = Convert.ToDecimal(reader["Percentile"])
                    };
                }
            }
            catch { }
            return new PeerBenchmark();
        }
    }
}