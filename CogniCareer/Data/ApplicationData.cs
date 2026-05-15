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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ApplicationData.InsertApplication: {ex.Message}");
                return 0;
            }
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
                while (reader.Read()) list.Add(MapApplication(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ApplicationData.GetByUser: {ex.Message}");
            }
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
                while (reader.Read()) list.Add(MapApplication(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ApplicationData.GetByJob: {ex.Message}");
            }
            return list;
        }

        public bool ApplicationExists(int userID, int jobID)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                // FIX: applications.student_id = students.id, not Users.UserID.
                // Resolve via students.user_id (populated by FIX_ONLY_RUN_THIS.sql).
                using var cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM dbo.applications
                      WHERE student_id = (SELECT id FROM dbo.students WHERE user_id = @user_id)
                      AND job_id = @job_id", con);
                cmd.Parameters.AddWithValue("@user_id", userID);
                cmd.Parameters.AddWithValue("@job_id", jobID);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ApplicationData.ApplicationExists: {ex.Message}");
                return false;
            }
        }

        public bool UpdateMatchScore(int applicationID, decimal score)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE dbo.applications SET match_score=@score WHERE id=@id", con);
                cmd.Parameters.AddWithValue("@score", score);
                cmd.Parameters.AddWithValue("@id", applicationID);
                con.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ApplicationData.UpdateMatchScore: {ex.Message}");
                return false;
            }
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ApplicationData.GetPeerBenchmark: {ex.Message}");
            }
            return new PeerBenchmark();
        }

        private static Application MapApplication(SqlDataReader r) => new Application
        {
            ApplicationID = Convert.ToInt32(r["ApplicationID"]),
            JobID = Convert.ToInt32(r["JobID"]),
            UserID = Convert.ToInt32(r["UserID"]),
            MatchScore = Convert.ToDecimal(r["MatchScore"]),
            AppliedAt = Convert.ToDateTime(r["AppliedAt"]),
            CurrentStatus = r["CurrentStatus"].ToString() ?? "",
            JobTitle = r["JobTitle"].ToString() ?? "",
            CompanyName = r["CompanyName"].ToString() ?? ""
        };
    }
}
