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

        public void EnrichDashboardStats(AdminStats stats)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                con.Open();

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.students", con))
                    stats.TotalStudents = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.companies WHERE status = 'active'", con))
                    stats.TotalCompanies = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM dbo.companies
                      WHERE status IS NULL OR status <> 'active'", con))
                    stats.PendingApprovals = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.jobs WHERE status = 'active'", con))
                    stats.TotalActiveJobs = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.applications", con))
                    stats.TotalApplications = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(AVG(CAST(match_score AS FLOAT)), 0) FROM dbo.applications", con))
                    stats.AverageMatchScore = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {GetType().Name}.EnrichDashboardStats: {ex.Message}");
            }
        }

        public List<AdminStudentRow> GetAllStudents()
        {
            var list = new List<AdminStudentRow>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(@"
SELECT s.id, s.user_id, s.full_name, s.email, s.university, s.status, s.joined_at,
       CASE
           WHEN u.UserID IS NOT NULL THEN CAST(u.IsActive AS INT)
           WHEN LOWER(ISNULL(s.status, 'active')) = 'active' THEN 1
           ELSE 0
       END AS is_active
FROM dbo.students s
LEFT JOIN dbo.Users u ON u.UserID = s.user_id
ORDER BY s.joined_at DESC, s.id DESC", con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new AdminStudentRow
                    {
                        StudentId = Convert.ToInt32(reader["id"]),
                        UserId = reader["user_id"] == DBNull.Value ? null : Convert.ToInt32(reader["user_id"]),
                        FullName = reader["full_name"].ToString() ?? "",
                        Email = reader["email"].ToString() ?? "",
                        University = reader["university"].ToString() ?? "",
                        IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                        JoinedAt = reader["joined_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["joined_at"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {GetType().Name}.GetAllStudents: {ex.Message}");
            }
            return list;
        }

        public bool ToggleStudentStatus(int studentId, bool isActive)
        {
            var status = isActive ? "active" : "inactive";
            try
            {
                using var con = DBHelper.GetConnection();
                con.Open();

                int? userId = null;
                using (var cmd = new SqlCommand(
                    "SELECT user_id FROM dbo.students WHERE id = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", studentId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        userId = Convert.ToInt32(result);
                }

                using (var cmd = new SqlCommand(
                    "UPDATE dbo.students SET status = @status WHERE id = @id", con))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", studentId);
                    cmd.ExecuteNonQuery();
                }

                if (userId.HasValue)
                {
                    using var cmd = new SqlCommand(
                        "UPDATE dbo.Users SET IsActive = @active WHERE UserID = @uid", con);
                    cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@uid", userId.Value);
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {GetType().Name}.ToggleStudentStatus: {ex.Message}");
                return false;
            }
        }

        public HomePublicStats GetPublicHomeStats()
        {
            var dashboard = GetStats() ?? new AdminStats();
            var home = new HomePublicStats
            {
                TotalStudents = dashboard.TotalStudents,
                TotalCompanies = dashboard.TotalCompanies,
                TotalActiveJobs = dashboard.TotalActiveJobs,
                TotalApplications = dashboard.TotalApplications,
                PendingApprovals = dashboard.PendingApprovals,
                MatchScorePercent = (int)Math.Round(dashboard.AverageMatchScore, MidpointRounding.AwayFromZero)
            };

            try
            {
                using var con = DBHelper.GetConnection();
                con.Open();

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.skills", con))
                    home.TotalSkills = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.applications WHERE CAST(applied_at AS DATE) = CAST(GETDATE() AS DATE)", con))
                    home.ApplicationsToday = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.jobs WHERE posted_at >= DATEADD(day, -7, GETDATE())", con))
                    home.JobsPostedThisWeek = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {GetType().Name}.GetPublicHomeStats: {ex.Message}");
            }

            return home;
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