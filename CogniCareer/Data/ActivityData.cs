using Microsoft.Data.SqlClient;
using CogniCareer.Models;

namespace CogniCareer.Data
{
    public class ActivityData
    {
        private static bool _schemaEnsured;

        public void EnsureSchema()
        {
            if (_schemaEnsured) return;
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'platform_activity' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.platform_activity (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_platform_activity PRIMARY KEY,
        event_type NVARCHAR(50) NOT NULL,
        title NVARCHAR(200) NOT NULL,
        description NVARCHAR(500) NULL,
        occurred_at DATETIME2 NOT NULL CONSTRAINT DF_platform_activity_occurred_at DEFAULT (SYSUTCDATETIME())
    );
    CREATE NONCLUSTERED INDEX IX_platform_activity_occurred_at
        ON dbo.platform_activity (occurred_at DESC);
END", con);
                con.Open();
                cmd.ExecuteNonQuery();
                _schemaEnsured = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ActivityData.EnsureSchema: {ex.Message}");
            }
        }

        public void Log(string eventType, string title, string description)
        {
            EnsureSchema();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    @"INSERT INTO dbo.platform_activity (event_type, title, description, occurred_at)
                      VALUES (@type, @title, @desc, SYSUTCDATETIME())", con);
                cmd.Parameters.AddWithValue("@type", eventType);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@desc", description);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ActivityData.Log: {ex.Message}");
            }
        }

        public void TouchStudentLogin(int userId)
        {
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "UPDATE dbo.students SET last_active_at = SYSUTCDATETIME() WHERE user_id = @uid", con);
                cmd.Parameters.AddWithValue("@uid", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ActivityData.TouchStudentLogin: {ex.Message}");
            }
        }

        public List<PlatformActivityEvent> GetRecent(DateTime? sinceUtc, int limit = 30)
        {
            EnsureSchema();
            var list = new List<PlatformActivityEvent>();
            try
            {
                using var con = DBHelper.GetConnection();
                using var cmd = new SqlCommand(@"
SELECT TOP (@limit) event_type, title, description, occurred_at
FROM (
    SELECT event_type, title, description, occurred_at
    FROM dbo.platform_activity
    WHERE (@since IS NULL OR occurred_at > @since)

    UNION ALL

    SELECT
        N'register',
        N'New student registration',
        CONCAT(s.full_name, N' joined', CASE WHEN s.university IS NULL OR s.university = N'' THEN N'' ELSE CONCAT(N' from ', s.university) END),
        s.joined_at
    FROM dbo.students s
    WHERE s.joined_at IS NOT NULL
      AND (@since IS NULL OR s.joined_at > @since)

    UNION ALL

    SELECT
        N'apply',
        N'Application submitted',
        CONCAT(s.full_name, N' applied to ', j.title, N' (', CAST(CAST(a.match_score AS INT) AS NVARCHAR(10)), N'% match)'),
        a.applied_at
    FROM dbo.applications a
    INNER JOIN dbo.students s ON s.id = a.student_id
    INNER JOIN dbo.jobs j ON j.id = a.job_id
    WHERE a.applied_at IS NOT NULL
      AND (@since IS NULL OR a.applied_at > @since)

    UNION ALL

    SELECT
        N'job',
        N'Job posted',
        CONCAT(c.name, N' posted ', j.title),
        j.posted_at
    FROM dbo.jobs j
    INNER JOIN dbo.companies c ON c.id = j.company_id
    WHERE j.posted_at IS NOT NULL
      AND (@since IS NULL OR j.posted_at > @since)

    UNION ALL

    SELECT
        N'company',
        N'Company registered',
        CONCAT(c.name, N' submitted for admin approval'),
        COALESCE(c.submitted_at, c.created_at)
    FROM dbo.companies c
    WHERE COALESCE(c.submitted_at, c.created_at) IS NOT NULL
      AND (@since IS NULL OR COALESCE(c.submitted_at, c.created_at) > @since)

    UNION ALL

    SELECT
        N'approved',
        N'Company approved',
        CONCAT(c.name, N' was approved by admin'),
        c.approved_at
    FROM dbo.companies c
    WHERE c.approved_at IS NOT NULL
      AND (@since IS NULL OR c.approved_at > @since)

    UNION ALL

    SELECT
        N'login',
        N'Student signed in',
        CONCAT(s.full_name, N' signed in to the student portal'),
        s.last_active_at
    FROM dbo.students s
    WHERE s.user_id IS NOT NULL
      AND s.last_active_at IS NOT NULL
      AND (@since IS NULL OR s.last_active_at > @since)
) AS events
ORDER BY occurred_at DESC", con);
                cmd.Parameters.AddWithValue("@limit", limit);
                cmd.Parameters.AddWithValue("@since", (object?)sinceUtc ?? DBNull.Value);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PlatformActivityEvent
                    {
                        EventType = reader["event_type"].ToString() ?? "",
                        Title = reader["title"].ToString() ?? "",
                        Description = reader["description"].ToString() ?? "",
                        OccurredAt = ReadUtc(reader["occurred_at"])
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ActivityData.GetRecent: {ex.Message}");
            }

            return list;
        }

        private static DateTime ReadUtc(object value)
        {
            var dt = Convert.ToDateTime(value);
            return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
    }
}
