using Microsoft.Data.SqlClient;
using CogniCareer.Models;
using System.Data;

namespace CogniCareer.Data
{
    public class AdminData
    {
        public AdminStats GetStats()
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand("sp_AdminGetDashboardStats", con);
                cmd.CommandType = CommandType.StoredProcedure;
                con.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new AdminStats
                    {
                        TotalStudents = Convert.ToInt32(reader["TotalStudents"]),
                        TotalCompanies = Convert.ToInt32(reader["TotalCompanies"]),
                        TotalActiveJobs = Convert.ToInt32(reader["TotalActiveJobs"]),
                        TotalApplications = Convert.ToInt32(reader["TotalApplications"]),
                        PendingApprovals = Convert.ToInt32(reader["PendingApprovals"]),
                        AverageMatchScore = Convert.ToDecimal(reader["AverageMatchScore"])
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {GetType().Name}: {ex.Message}");
                return null; // keep whatever return was there originally
            }
            return new AdminStats();
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {GetType().Name}: {ex.Message}");
                return null; // keep whatever return was there originally
            }
            return list;
        }

        public List<SkillInsight> GetTopDemandingSkills(int top = 5)
        {
            var list = new List<SkillInsight>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    @"SELECT TOP (@top) name, category,
                             ISNULL(jobs_count, 0) AS jobs_count,
                             ISNULL(demand_pct, 0) AS demand_pct
                      FROM dbo.skills
                      ORDER BY jobs_count DESC, demand_pct DESC, name", con);
                cmd.Parameters.AddWithValue("@top", top);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new SkillInsight
                    {
                        SkillName = reader["name"].ToString() ?? "",
                        Category = reader["category"].ToString() ?? "",
                        JobCount = Convert.ToInt32(reader["jobs_count"]),
                        DemandPct = Convert.ToInt32(reader["demand_pct"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] AdminData.GetTopDemandingSkills: {ex.Message}");
            }
            return list;
        }

        public List<SkillInsight> GetMostMissingSkills(int top = 5)
        {
            var list = new List<SkillInsight>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    @"SELECT TOP (@top) s.name, s.category,
                             COUNT(DISTINCT js.JobID) AS job_count,
                             (SELECT COUNT(DISTINCT ss.UserID) FROM StudentSkills ss WHERE ss.SkillID = s.id) AS student_count
                      FROM dbo.skills s
                      INNER JOIN JobSkills js ON js.SkillID = s.id
                      GROUP BY s.id, s.name, s.category
                      ORDER BY student_count ASC, job_count DESC", con);
                cmd.Parameters.AddWithValue("@top", top);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new SkillInsight
                    {
                        SkillName = reader["name"].ToString() ?? "",
                        Category = reader["category"].ToString() ?? "",
                        JobCount = Convert.ToInt32(reader["job_count"]),
                        StudentCount = Convert.ToInt32(reader["student_count"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] AdminData.GetMostMissingSkills (JobSkills): {ex.Message}");
                try
                {
                    using var con = DBHelper.GetConnection();
                    using var cmd = new SqlCommand(
                        @"SELECT TOP (@top) s.name, s.category,
                                 ISNULL(s.jobs_count, 0) AS job_count,
                                 0 AS student_count
                          FROM dbo.skills s
                          ORDER BY s.jobs_count DESC, s.name", con);
                    cmd.Parameters.AddWithValue("@top", top);
                    con.Open();
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new SkillInsight
                        {
                            SkillName = reader["name"].ToString() ?? "",
                            Category = reader["category"].ToString() ?? "",
                            JobCount = Convert.ToInt32(reader["job_count"]),
                            StudentCount = Convert.ToInt32(reader["student_count"])
                        });
                    }
                }
                catch { }
            }
            return list;
        }

        public List<CompanyInsight> GetTopCompanies(int top = 5)
        {
            var list = new List<CompanyInsight>();
            string[] queries =
            {
                @"SELECT TOP (@top) c.CompanyName,
                         COUNT(DISTINCT j.id) AS job_count,
                         COUNT(a.id) AS app_count
                  FROM dbo.vw_Companies c
                  LEFT JOIN dbo.jobs j ON j.company_id = c.CompanyID
                  LEFT JOIN dbo.applications a ON a.job_id = j.id
                  WHERE c.IsApproved = 1
                  GROUP BY c.CompanyID, c.CompanyName
                  HAVING COUNT(DISTINCT j.id) > 0 OR COUNT(a.id) > 0
                  ORDER BY app_count DESC, job_count DESC",
                @"SELECT TOP (@top) c.CompanyName,
                         COUNT(DISTINCT j.JobID) AS job_count,
                         COUNT(a.ApplicationID) AS app_count
                  FROM dbo.vw_Companies c
                  LEFT JOIN Jobs j ON j.CompanyID = c.CompanyID
                  LEFT JOIN Applications a ON a.JobID = j.JobID
                  WHERE c.IsApproved = 1
                  GROUP BY c.CompanyID, c.CompanyName
                  HAVING COUNT(DISTINCT j.JobID) > 0 OR COUNT(a.ApplicationID) > 0
                  ORDER BY app_count DESC, job_count DESC",
                @"SELECT TOP (@top) c.CompanyName,
                         COUNT(DISTINCT j.id) AS job_count,
                         0 AS app_count
                  FROM dbo.vw_Companies c
                  INNER JOIN dbo.jobs j ON j.company_id = c.CompanyID
                  WHERE c.IsApproved = 1
                  GROUP BY c.CompanyID, c.CompanyName
                  ORDER BY job_count DESC"
            };

            foreach (var sql in queries)
            {
                if (list.Count >= top) break;
                try
                {
                    using var con = DBHelper.GetConnection();
                    using var cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@top", top);
                    con.Open();
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new CompanyInsight
                        {
                            CompanyName = reader["CompanyName"].ToString() ?? "",
                            JobCount = Convert.ToInt32(reader["job_count"]),
                            ApplicationCount = Convert.ToInt32(reader["app_count"])
                        });
                    }
                    if (list.Count > 0) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] AdminData.GetTopCompanies: {ex.Message}");
                }
            }
            return list;
        }
    }
}