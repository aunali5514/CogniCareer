/* ============================================================
   CogniCareerDB — FINAL CLEAN SCRIPT
   Run this once in SSMS on a fresh instance.
   It creates the database, all tables, stored procedures,
   trigger, and realistic seed data.
   Password for ALL seed users: Admin@123
   ============================================================ */

/* ── 1. CREATE / USE DATABASE ────────────────────────────── */
IF DB_ID('CogniCareerDB') IS NULL
    CREATE DATABASE CogniCareerDB;
GO
USE CogniCareerDB;
GO

/* ── 2. DROP TABLES (clean slate, correct order) ─────────── */
IF OBJECT_ID('dbo.ApplicationStatusLog','U') IS NOT NULL DROP TABLE dbo.ApplicationStatusLog;
IF OBJECT_ID('dbo.ApplicationNotes',    'U') IS NOT NULL DROP TABLE dbo.ApplicationNotes;
IF OBJECT_ID('dbo.Alerts',              'U') IS NOT NULL DROP TABLE dbo.Alerts;
IF OBJECT_ID('dbo.Applications',        'U') IS NOT NULL DROP TABLE dbo.Applications;
IF OBJECT_ID('dbo.JobSkills',           'U') IS NOT NULL DROP TABLE dbo.JobSkills;
IF OBJECT_ID('dbo.Jobs',                'U') IS NOT NULL DROP TABLE dbo.Jobs;
IF OBJECT_ID('dbo.StudentSkills',       'U') IS NOT NULL DROP TABLE dbo.StudentSkills;
IF OBJECT_ID('dbo.LearningResources',   'U') IS NOT NULL DROP TABLE dbo.LearningResources;
IF OBJECT_ID('dbo.Skills',              'U') IS NOT NULL DROP TABLE dbo.Skills;
IF OBJECT_ID('dbo.StudentProfiles',     'U') IS NOT NULL DROP TABLE dbo.StudentProfiles;
IF OBJECT_ID('dbo.Companies',           'U') IS NOT NULL DROP TABLE dbo.Companies;
IF OBJECT_ID('dbo.Users',               'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.sqlUsers',            'U') IS NOT NULL DROP TABLE dbo.sqlUsers;
GO

/* ── 3. CREATE TABLES ────────────────────────────────────── */

CREATE TABLE dbo.Users (
    UserID       INT           IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(150) NOT NULL,
    Email        NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL CHECK (Role IN ('Student','Company','Admin')),
    IsActive     BIT           NOT NULL DEFAULT(1),
    CreatedAt    DATETIME2     NOT NULL DEFAULT(GETDATE())
);
GO

CREATE TABLE dbo.StudentProfiles (
    ProfileID          INT          IDENTITY(1,1) PRIMARY KEY,
    UserID             INT          NOT NULL,
    University         NVARCHAR(200) NOT NULL,
    Degree             NVARCHAR(150) NOT NULL,
    Semester           INT          NOT NULL,
    GPA                DECIMAL(3,2) NOT NULL,
    ExpectedGradYear   INT          NOT NULL,
    IsProfileComplete  BIT          NOT NULL DEFAULT(0),
    CONSTRAINT FK_StudentProfiles_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
);
GO

CREATE TABLE dbo.Companies (
    CompanyID   INT           IDENTITY(1,1) PRIMARY KEY,
    UserID      INT           NOT NULL,
    CompanyName NVARCHAR(200) NOT NULL,
    Industry    NVARCHAR(100) NOT NULL,
    Website     NVARCHAR(300) NULL,
    Description NVARCHAR(1000) NULL,
    IsApproved  BIT           NOT NULL DEFAULT(0),
    ApprovedAt  DATETIME2     NULL,
    CONSTRAINT FK_Companies_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
);
GO

CREATE TABLE dbo.Skills (
    SkillID   INT           IDENTITY(1,1) PRIMARY KEY,
    SkillName NVARCHAR(150) NOT NULL UNIQUE,
    Category  NVARCHAR(100) NOT NULL,
    IsActive  BIT           NOT NULL DEFAULT(1)
);
GO

CREATE TABLE dbo.LearningResources (
    ResourceID INT           IDENTITY(1,1) PRIMARY KEY,
    SkillID    INT           NOT NULL,
    Title      NVARCHAR(300) NOT NULL,
    URL        NVARCHAR(700) NOT NULL,
    Platform   NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_LearningResources_Skills FOREIGN KEY (SkillID) REFERENCES dbo.Skills(SkillID)
);
GO

CREATE TABLE dbo.StudentSkills (
    StudentSkillID  INT          IDENTITY(1,1) PRIMARY KEY,
    UserID          INT          NOT NULL,
    SkillID         INT          NOT NULL,
    ProficiencyLevel NVARCHAR(20) NOT NULL CHECK (ProficiencyLevel IN ('Beginner','Intermediate','Advanced')),
    CONSTRAINT FK_StudentSkills_Users  FOREIGN KEY (UserID)  REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_StudentSkills_Skills FOREIGN KEY (SkillID) REFERENCES dbo.Skills(SkillID),
    CONSTRAINT UQ_StudentSkills_User_Skill UNIQUE (UserID, SkillID)
);
GO

CREATE TABLE dbo.Jobs (
    JobID       INT            IDENTITY(1,1) PRIMARY KEY,
    CompanyID   INT            NOT NULL,
    Title       NVARCHAR(200)  NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    JobType     NVARCHAR(50)   NOT NULL,
    Duration    NVARCHAR(50)   NOT NULL,
    Deadline    DATE           NOT NULL,
    Status      NVARCHAR(30)   NOT NULL DEFAULT('Active'),
    PostedAt    DATETIME2      NOT NULL DEFAULT(GETDATE()),
    CONSTRAINT FK_Jobs_Users FOREIGN KEY (CompanyID) REFERENCES dbo.Users(UserID)
);
GO

CREATE TABLE dbo.JobSkills (
    JobSkillID INT          IDENTITY(1,1) PRIMARY KEY,
    JobID      INT          NOT NULL,
    SkillID    INT          NOT NULL,
    Priority   NVARCHAR(20) NOT NULL CHECK (Priority IN ('Required','Preferred','Bonus')),
    CONSTRAINT FK_JobSkills_Jobs   FOREIGN KEY (JobID)   REFERENCES dbo.Jobs(JobID),
    CONSTRAINT FK_JobSkills_Skills FOREIGN KEY (SkillID) REFERENCES dbo.Skills(SkillID)
);
GO

CREATE TABLE dbo.Applications (
    ApplicationID INT          IDENTITY(1,1) PRIMARY KEY,
    JobID         INT          NOT NULL,
    UserID        INT          NOT NULL,
    MatchScore    DECIMAL(5,2) NOT NULL,
    AppliedAt     DATETIME2    NOT NULL DEFAULT(GETDATE()),
    CurrentStatus NVARCHAR(30) NOT NULL DEFAULT('Applied'),
    CONSTRAINT FK_Applications_Jobs  FOREIGN KEY (JobID)  REFERENCES dbo.Jobs(JobID),
    CONSTRAINT FK_Applications_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT UQ_Applications_Job_User UNIQUE (JobID, UserID)
);
GO

CREATE TABLE dbo.ApplicationStatusLog (
    LogID           INT          IDENTITY(1,1) PRIMARY KEY,
    ApplicationID   INT          NOT NULL,
    Status          NVARCHAR(30) NOT NULL,
    ChangedAt       DATETIME2    NOT NULL DEFAULT(GETDATE()),
    ChangedByUserID INT          NULL,
    CONSTRAINT FK_AppStatusLog_Applications FOREIGN KEY (ApplicationID)   REFERENCES dbo.Applications(ApplicationID),
    CONSTRAINT FK_AppStatusLog_Users        FOREIGN KEY (ChangedByUserID) REFERENCES dbo.Users(UserID)
);
GO

CREATE TABLE dbo.ApplicationNotes (
    NoteID        INT            IDENTITY(1,1) PRIMARY KEY,
    ApplicationID INT            NOT NULL,
    NoteText      NVARCHAR(2000) NOT NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT(GETDATE()),
    CONSTRAINT FK_ApplicationNotes_Applications FOREIGN KEY (ApplicationID) REFERENCES dbo.Applications(ApplicationID)
);
GO

CREATE TABLE dbo.Alerts (
    AlertID   INT           IDENTITY(1,1) PRIMARY KEY,
    UserID    INT           NOT NULL,
    Message   NVARCHAR(300) NOT NULL,
    AlertType NVARCHAR(50)  NOT NULL,
    IsRead    BIT           NOT NULL DEFAULT(0),
    CreatedAt DATETIME2     NOT NULL DEFAULT(GETDATE()),
    CONSTRAINT FK_Alerts_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
);
GO

/* ── 4. STORED PROCEDURES ────────────────────────────────── */

CREATE OR ALTER PROCEDURE sp_RegisterUser
    @FullName     NVARCHAR(100),
    @Email        NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @Role         NVARCHAR(20)
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN SELECT 0; RETURN; END
    INSERT INTO Users (FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (@FullName, @Email, @PasswordHash, @Role, 1, GETDATE());
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_GetUserByEmail
    @Email NVARCHAR(100)
AS
BEGIN
    SELECT * FROM Users WHERE Email = @Email;
END
GO

CREATE OR ALTER PROCEDURE sp_GetUserByID
    @UserID INT
AS
BEGIN
    SELECT * FROM Users WHERE UserID = @UserID;
END
GO

CREATE OR ALTER PROCEDURE sp_DeactivateUser
    @UserID   INT,
    @IsActive BIT
AS
BEGIN
    UPDATE Users SET IsActive = @IsActive WHERE UserID = @UserID;
END
GO

CREATE OR ALTER PROCEDURE sp_InsertStudentProfile
    @UserID            INT,
    @University        NVARCHAR(100),
    @Degree            NVARCHAR(100),
    @Semester          INT,
    @GPA               DECIMAL(3,2),
    @ExpectedGradYear  INT,
    @IsProfileComplete BIT
AS
BEGIN
    INSERT INTO StudentProfiles (UserID, University, Degree, Semester, GPA, ExpectedGradYear, IsProfileComplete)
    VALUES (@UserID, @University, @Degree, @Semester, @GPA, @ExpectedGradYear, @IsProfileComplete);
END
GO

CREATE OR ALTER PROCEDURE sp_GetStudentProfile
    @UserID INT
AS
BEGIN
    SELECT * FROM StudentProfiles WHERE UserID = @UserID;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateStudentProfile
    @UserID            INT,
    @University        NVARCHAR(100),
    @Degree            NVARCHAR(100),
    @Semester          INT,
    @GPA               DECIMAL(3,2),
    @ExpectedGradYear  INT,
    @IsProfileComplete BIT
AS
BEGIN
    UPDATE StudentProfiles
    SET University = @University, Degree = @Degree, Semester = @Semester,
        GPA = @GPA, ExpectedGradYear = @ExpectedGradYear, IsProfileComplete = @IsProfileComplete
    WHERE UserID = @UserID;
END
GO

CREATE OR ALTER PROCEDURE sp_InsertCompany
    @UserID      INT,
    @CompanyName NVARCHAR(100),
    @Industry    NVARCHAR(50),
    @Website     NVARCHAR(200),
    @Description NVARCHAR(500)
AS
BEGIN
    INSERT INTO Companies (UserID, CompanyName, Industry, Website, Description, IsApproved)
    VALUES (@UserID, @CompanyName, @Industry, @Website, @Description, 0);
END
GO

CREATE OR ALTER PROCEDURE sp_GetPendingCompanies
AS
BEGIN
    SELECT * FROM Companies WHERE IsApproved = 0;
END
GO

CREATE OR ALTER PROCEDURE sp_ApproveCompany
    @CompanyID INT
AS
BEGIN
    UPDATE Companies SET IsApproved = 1, ApprovedAt = GETDATE() WHERE CompanyID = @CompanyID;
END
GO

CREATE OR ALTER PROCEDURE sp_RejectCompany
    @CompanyID INT
AS
BEGIN
    DELETE FROM Companies WHERE CompanyID = @CompanyID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetStudentSkills
    @UserID INT
AS
BEGIN
    SELECT ss.StudentSkillID, ss.UserID, ss.SkillID, s.SkillName, s.Category, ss.ProficiencyLevel
    FROM StudentSkills ss
    JOIN Skills s ON ss.SkillID = s.SkillID
    WHERE ss.UserID = @UserID;
END
GO

CREATE OR ALTER PROCEDURE sp_AddStudentSkill
    @UserID          INT,
    @SkillID         INT,
    @ProficiencyLevel NVARCHAR(20)
AS
BEGIN
    INSERT INTO StudentSkills (UserID, SkillID, ProficiencyLevel)
    VALUES (@UserID, @SkillID, @ProficiencyLevel);
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateStudentSkill
    @StudentSkillID  INT,
    @ProficiencyLevel NVARCHAR(20)
AS
BEGIN
    UPDATE StudentSkills SET ProficiencyLevel = @ProficiencyLevel
    WHERE StudentSkillID = @StudentSkillID;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteStudentSkill
    @StudentSkillID INT
AS
BEGIN
    DELETE FROM StudentSkills WHERE StudentSkillID = @StudentSkillID;
END
GO

CREATE OR ALTER PROCEDURE sp_InsertJob
    @CompanyID   INT,
    @Title       NVARCHAR(100),
    @Description NVARCHAR(1000),
    @JobType     NVARCHAR(50),
    @Duration    NVARCHAR(50),
    @Deadline    DATE
AS
BEGIN
    INSERT INTO Jobs (CompanyID, Title, Description, JobType, Duration, Deadline, Status, PostedAt)
    VALUES (@CompanyID, @Title, @Description, @JobType, @Duration, @Deadline, 'Active', GETDATE());
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_GetJobsByCompany
    @CompanyID INT
AS
BEGIN
    SELECT j.*, c.CompanyName
    FROM Jobs j
    JOIN Companies c ON j.CompanyID = c.CompanyID
    WHERE j.CompanyID = @CompanyID
    ORDER BY j.PostedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetAllActiveJobs
AS
BEGIN
    SELECT j.*, c.CompanyName
    FROM Jobs j
    JOIN Companies c ON j.CompanyID = c.CompanyID
    WHERE j.Status = 'Active'
    ORDER BY j.PostedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateJob
    @JobID       INT,
    @Title       NVARCHAR(100),
    @Description NVARCHAR(1000),
    @JobType     NVARCHAR(50),
    @Duration    NVARCHAR(50),
    @Deadline    DATE
AS
BEGIN
    UPDATE Jobs
    SET Title = @Title, Description = @Description, JobType = @JobType,
        Duration = @Duration, Deadline = @Deadline
    WHERE JobID = @JobID;
END
GO

CREATE OR ALTER PROCEDURE sp_CloseJob
    @JobID INT
AS
BEGIN
    UPDATE Jobs SET Status = 'Closed' WHERE JobID = @JobID;
END
GO

CREATE OR ALTER PROCEDURE sp_DeactivateJob
    @JobID INT
AS
BEGIN
    UPDATE Jobs SET Status = 'Inactive' WHERE JobID = @JobID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetSkillsByJob
    @JobID INT
AS
BEGIN
    SELECT js.JobSkillID, js.JobID, js.SkillID, s.SkillName, js.Priority
    FROM JobSkills js
    JOIN Skills s ON js.SkillID = s.SkillID
    WHERE js.JobID = @JobID;
END
GO

CREATE OR ALTER PROCEDURE sp_AddJobSkill
    @JobID    INT,
    @SkillID  INT,
    @Priority NVARCHAR(20)
AS
BEGIN
    INSERT INTO JobSkills (JobID, SkillID, Priority)
    VALUES (@JobID, @SkillID, @Priority);
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteJobSkillsByJob
    @JobID INT
AS
BEGIN
    DELETE FROM JobSkills WHERE JobID = @JobID;
END
GO

CREATE OR ALTER PROCEDURE sp_InsertApplication
    @JobID      INT,
    @UserID     INT,
    @MatchScore DECIMAL(5,2)
AS
BEGIN
    INSERT INTO Applications (JobID, UserID, MatchScore, AppliedAt, CurrentStatus)
    VALUES (@JobID, @UserID, @MatchScore, GETDATE(), 'Applied');
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_GetApplicationsByUser
    @UserID INT
AS
BEGIN
    SELECT a.*, j.Title AS JobTitle, c.CompanyName
    FROM Applications a
    JOIN Jobs j      ON a.JobID      = j.JobID
    JOIN Companies c ON j.CompanyID  = c.CompanyID
    WHERE a.UserID = @UserID
    ORDER BY a.AppliedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_GetApplicationsByJob
    @JobID INT
AS
BEGIN
    SELECT a.*, j.Title AS JobTitle, c.CompanyName
    FROM Applications a
    JOIN Jobs j      ON a.JobID     = j.JobID
    JOIN Companies c ON j.CompanyID = c.CompanyID
    WHERE a.JobID = @JobID
    ORDER BY a.MatchScore DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_InsertApplicationStatus
    @ApplicationID    INT,
    @Status           NVARCHAR(50),
    @ChangedByUserID  INT
AS
BEGIN
    INSERT INTO ApplicationStatusLog (ApplicationID, Status, ChangedAt, ChangedByUserID)
    VALUES (@ApplicationID, @Status, GETDATE(), @ChangedByUserID);
    UPDATE Applications SET CurrentStatus = @Status WHERE ApplicationID = @ApplicationID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetApplicationHistory
    @ApplicationID INT
AS
BEGIN
    SELECT * FROM ApplicationStatusLog
    WHERE ApplicationID = @ApplicationID
    ORDER BY ChangedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AddApplicationNote
    @ApplicationID INT,
    @NoteText      NVARCHAR(500)
AS
BEGIN
    INSERT INTO ApplicationNotes (ApplicationID, NoteText, CreatedAt)
    VALUES (@ApplicationID, @NoteText, GETDATE());
END
GO

CREATE OR ALTER PROCEDURE sp_GetApplicationNotes
    @ApplicationID INT
AS
BEGIN
    SELECT * FROM ApplicationNotes
    WHERE ApplicationID = @ApplicationID
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_InsertAlert
    @UserID    INT,
    @Message   NVARCHAR(300),
    @AlertType NVARCHAR(50)
AS
BEGIN
    INSERT INTO Alerts (UserID, Message, AlertType, IsRead, CreatedAt)
    VALUES (@UserID, @Message, @AlertType, 0, GETDATE());
END
GO

CREATE OR ALTER PROCEDURE sp_GetUnreadAlerts
    @UserID INT
AS
BEGIN
    SELECT * FROM Alerts
    WHERE UserID = @UserID AND IsRead = 0
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_MarkAlertRead
    @AlertID INT
AS
BEGIN
    UPDATE Alerts SET IsRead = 1 WHERE AlertID = @AlertID;
END
GO

CREATE OR ALTER PROCEDURE sp_MarkAllAlertsRead
    @UserID INT
AS
BEGIN
    UPDATE Alerts SET IsRead = 1 WHERE UserID = @UserID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetPeerBenchmark
    @UserID INT,
    @JobID  INT
AS
BEGIN
    SELECT
        COUNT(*) AS TotalApplicants,
        MAX(MatchScore) AS TopScore,
        ISNULL((SELECT MatchScore FROM Applications WHERE UserID = @UserID AND JobID = @JobID), 0) AS MyScore,
        ISNULL(
            ROUND(
                (SELECT COUNT(*) FROM Applications
                 WHERE JobID = @JobID AND MatchScore <=
                       (SELECT ISNULL(MatchScore,0) FROM Applications WHERE UserID = @UserID AND JobID = @JobID))
                * 100.0 / NULLIF(COUNT(*), 0), 1
            ), 0
        ) AS Percentile
    FROM Applications
    WHERE JobID = @JobID;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetDashboardStats
AS
BEGIN
    SELECT
        (SELECT COUNT(*) FROM Users       WHERE Role   = 'Student')  AS TotalStudents,
        (SELECT COUNT(*) FROM Companies   WHERE IsApproved = 1)       AS TotalCompanies,
        (SELECT COUNT(*) FROM Jobs        WHERE Status  = 'Active')   AS TotalActiveJobs,
        (SELECT COUNT(*) FROM Applications)                            AS TotalApplications,
        (SELECT COUNT(*) FROM Companies   WHERE IsApproved = 0)       AS PendingApprovals,
        ISNULL((SELECT ROUND(AVG(CAST(MatchScore AS FLOAT)),1) FROM Applications), 0) AS AverageMatchScore;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetMostAppliedJobs
AS
BEGIN
    SELECT TOP 10 j.Title, c.CompanyName, COUNT(*) AS ApplicationCount
    FROM Applications a
    JOIN Jobs j      ON a.JobID     = j.JobID
    JOIN Companies c ON j.CompanyID = c.CompanyID
    GROUP BY j.Title, c.CompanyName
    ORDER BY ApplicationCount DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetTopSkillsInDemand
AS
BEGIN
    SELECT TOP 10 s.SkillName, s.Category, COUNT(*) AS JobCount
    FROM JobSkills js
    JOIN Skills s ON js.SkillID = s.SkillID
    GROUP BY s.SkillName, s.Category
    ORDER BY JobCount DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetMostActiveCompanies
AS
BEGIN
    SELECT TOP 10 c.CompanyName, COUNT(j.JobID) AS JobCount
    FROM Companies c
    LEFT JOIN Jobs j ON j.CompanyID = c.UserID
    WHERE c.IsApproved = 1
    GROUP BY c.CompanyName
    ORDER BY JobCount DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetJobsByType
AS
BEGIN
    SELECT JobType, COUNT(*) AS Total
    FROM Jobs
    GROUP BY JobType;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetJobsClosingThisWeek
AS
BEGIN
    SELECT j.JobID, j.Title, c.CompanyName, j.Deadline
    FROM Jobs j
    JOIN Companies c ON j.CompanyID = c.CompanyID
    WHERE j.Status = 'Active'
      AND j.Deadline BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE))
    ORDER BY j.Deadline;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetJobsWithZeroApplicants
AS
BEGIN
    SELECT j.JobID, j.Title, c.CompanyName, j.PostedAt
    FROM Jobs j
    JOIN Companies c ON j.CompanyID = c.CompanyID
    WHERE j.Status = 'Active'
      AND NOT EXISTS (SELECT 1 FROM Applications a WHERE a.JobID = j.JobID)
    ORDER BY j.PostedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetHiringRatioPerCompany
AS
BEGIN
    SELECT c.CompanyName,
           COUNT(a.ApplicationID)                                    AS TotalApplications,
           SUM(CASE WHEN a.CurrentStatus = 'Hired' THEN 1 ELSE 0 END) AS TotalHired,
           ISNULL(ROUND(SUM(CASE WHEN a.CurrentStatus='Hired' THEN 1.0 ELSE 0 END)
                  / NULLIF(COUNT(a.ApplicationID),0) * 100, 1), 0)  AS HiringRatioPct
    FROM Companies c
    LEFT JOIN Jobs j        ON j.CompanyID = c.UserID
    LEFT JOIN Applications a ON a.JobID    = j.JobID
    WHERE c.IsApproved = 1
    GROUP BY c.CompanyName
    ORDER BY HiringRatioPct DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AdminGetMostCommonMissingSkill
AS
BEGIN
    SELECT TOP 10 s.SkillName, COUNT(*) AS MissingCount
    FROM Applications a
    JOIN Jobs j         ON a.JobID   = j.JobID
    JOIN JobSkills js   ON js.JobID  = j.JobID
    JOIN Skills s       ON s.SkillID = js.SkillID
    WHERE js.Priority = 'Required'
      AND NOT EXISTS (
            SELECT 1 FROM StudentSkills ss
            WHERE ss.UserID  = a.UserID
              AND ss.SkillID = js.SkillID
          )
    GROUP BY s.SkillName
    ORDER BY MissingCount DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_AddSkill
    @SkillName NVARCHAR(100),
    @Category  NVARCHAR(50)
AS
BEGIN
    INSERT INTO Skills (SkillName, Category, IsActive)
    VALUES (@SkillName, @Category, 1);
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateSkill
    @SkillID   INT,
    @SkillName NVARCHAR(100),
    @Category  NVARCHAR(50)
AS
BEGIN
    UPDATE Skills SET SkillName = @SkillName, Category = @Category
    WHERE SkillID = @SkillID;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteSkill
    @SkillID INT
AS
BEGIN
    UPDATE Skills SET IsActive = 0 WHERE SkillID = @SkillID;
END
GO

CREATE OR ALTER PROCEDURE sp_AddLearningResource
    @SkillID  INT,
    @Title    NVARCHAR(300),
    @URL      NVARCHAR(700),
    @Platform NVARCHAR(100)
AS
BEGIN
    INSERT INTO LearningResources (SkillID, Title, URL, Platform)
    VALUES (@SkillID, @Title, @URL, @Platform);
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateLearningResource
    @ResourceID INT,
    @Title      NVARCHAR(300),
    @URL        NVARCHAR(700),
    @Platform   NVARCHAR(100)
AS
BEGIN
    UPDATE LearningResources
    SET Title = @Title, URL = @URL, Platform = @Platform
    WHERE ResourceID = @ResourceID;
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteLearningResource
    @ResourceID INT
AS
BEGIN
    DELETE FROM LearningResources WHERE ResourceID = @ResourceID;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateMatchScore
    @ApplicationID INT,
    @MatchScore    DECIMAL(5,2)
AS
BEGIN
    UPDATE Applications SET MatchScore = @MatchScore
    WHERE ApplicationID = @ApplicationID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetStudentProfile_Full
    @UserID INT
AS
BEGIN
    SELECT u.UserID, u.FullName, u.Email, u.Role,
           sp.University, sp.Degree, sp.Semester, sp.GPA,
           sp.ExpectedGradYear, sp.IsProfileComplete
    FROM Users u
    LEFT JOIN StudentProfiles sp ON sp.UserID = u.UserID
    WHERE u.UserID = @UserID;
END
GO

/* ── 5. TRIGGER ──────────────────────────────────────────── */
CREATE OR ALTER TRIGGER dbo.trg_LogStatusChange
ON dbo.Applications
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ApplicationStatusLog (ApplicationID, Status, ChangedAt, ChangedByUserID)
    SELECT i.ApplicationID, i.CurrentStatus, GETDATE(), NULL
    FROM inserted i
    INNER JOIN deleted d ON d.ApplicationID = i.ApplicationID
    WHERE ISNULL(i.CurrentStatus,'') <> ISNULL(d.CurrentStatus,'');
END
GO

/* ── 6. SEED DATA ────────────────────────────────────────── */
-- PasswordHash below = BCrypt hash of "Admin@123"
-- Generated with BCrypt.Net.BCrypt.HashPassword("Admin@123")

SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (UserID, FullName, Email, PasswordHash, Role, IsActive, CreatedAt) VALUES
(1,  'Platform Admin',        'admin@cognicareer.com',          '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Admin',  1,'2024-01-01 08:00:00'),
(2,  'Arbisoft HR',           'hr@arbisoft.com',                '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-05 09:00:00'),
(3,  'Systems Ltd Recruiter', 'talent@systemsltd.com',          '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-06 09:30:00'),
(4,  'Netsol Talent Team',    'jobs@netsoltech.com',            '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-07 10:00:00'),
(5,  'TRG Hiring',            'careers@trg.com.pk',             '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-08 10:00:00'),
(6,  'Telenor HR Pakistan',   'hr.pk@telenor.com',              '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-09 10:30:00'),
(7,  'Careem Talent',         'talent@careem.com',              '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-10 11:00:00'),
(8,  'Avanza Solutions HR',   'hr@avanzasolutions.com',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-11 11:00:00'),
(9,  'Devsinc Recruiting',    'hiring@devsinc.com',             '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-12 11:30:00'),
(10, 'Techlogix HR',          'careers@techlogix.com',          '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-13 12:00:00'),
(11, 'VentureDive Talent',    'hiring@venturedive.com',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-14 12:00:00'),
(12, 'Folio3 HR',             'hr@folio3.com',                  '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-15 12:30:00'),
(13, 'Afiniti Recruiting',    'recruiting@afiniti.com',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-16 13:00:00'),
(14, 'Inbox Business Tech HR','jobs@inboxbusiness.com',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-17 13:00:00'),
(15, 'Vteam Talent',          'talent@vteams.com',              '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',1,'2024-01-18 13:30:00'),
(16, 'Confiz HR',             'hr@confiz.com',                  '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Company',0,'2024-01-19 14:00:00'),
(17, 'Ali Hassan',            'ali.hassan@student.uet.edu.pk',  '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-01 09:00:00'),
(18, 'Sara Malik',            'sara.malik@student.fast.edu.pk', '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-02 09:15:00'),
(19, 'Usman Raza',            'usman.raza@lums.edu.pk',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-03 09:30:00'),
(20, 'Ayesha Tariq',          'ayesha.tariq@nust.edu.pk',       '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-04 09:45:00'),
(21, 'Hamza Shahid',          'hamza.shahid@pu.edu.pk',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-05 10:00:00'),
(22, 'Zainab Iqbal',          'zainab.iqbal@itu.edu.pk',        '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-06 10:15:00'),
(23, 'Bilal Aslam',           'bilal.aslam@comsats.edu.pk',     '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-07 10:30:00'),
(24, 'Fatima Noor',           'fatima.noor@giki.edu.pk',        '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-08 10:45:00'),
(25, 'Omar Farooq',           'omar.farooq@student.uet.edu.pk', '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-09 11:00:00'),
(26, 'Hina Baig',             'hina.baig@student.fast.edu.pk',  '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-10 11:15:00'),
(27, 'Talha Mahmood',         'talha.mahmood@nust.edu.pk',      '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-11 11:30:00'),
(28, 'Amna Siddiqui',         'amna.siddiqui@itu.edu.pk',       '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-12 11:45:00'),
(29, 'Faisal Kamran',         'faisal.kamran@lums.edu.pk',      '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-13 12:00:00'),
(30, 'Mariam Waheed',         'mariam.waheed@comsats.edu.pk',   '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-14 12:15:00'),
(31, 'Hassan Javed',          'hassan.javed@pu.edu.pk',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-15 12:30:00'),
(32, 'Sana Butt',             'sana.butt@giki.edu.pk',          '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-16 12:45:00'),
(33, 'Adeel Mirza',           'adeel.mirza@student.uet.edu.pk', '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-17 13:00:00'),
(34, 'Rabia Zahid',           'rabia.zahid@student.fast.edu.pk','$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-18 13:15:00'),
(35, 'Muzammil Shah',         'muzammil.shah@nust.edu.pk',      '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-19 13:30:00'),
(36, 'Iqra Khalid',           'iqra.khalid@itu.edu.pk',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-20 13:45:00'),
(37, 'Shoaib Anwar',          'shoaib.anwar@comsats.edu.pk',    '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-21 14:00:00'),
(38, 'Nadia Islam',           'nadia.islam@lums.edu.pk',        '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-22 14:15:00'),
(39, 'Waleed Sohail',         'waleed.sohail@giki.edu.pk',      '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-23 14:30:00'),
(40, 'Asma Nawaz',            'asma.nawaz@pu.edu.pk',           '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-24 14:45:00'),
(41, 'Junaid Qadir',          'junaid.qadir@student.uet.edu.pk','$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-25 15:00:00'),
(42, 'Mehwish Ali',           'mehwish.ali@student.fast.edu.pk','$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-26 15:15:00'),
(43, 'Tariq Naeem',           'tariq.naeem@nust.edu.pk',        '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-27 15:30:00'),
(44, 'Lubna Rashid',          'lubna.rashid@comsats.edu.pk',    '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-02-28 15:45:00'),
(45, 'Nasir Ullah',           'nasir.ullah@itu.edu.pk',         '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-03-01 09:00:00'),
(46, 'Sadia Perveen',         'sadia.perveen@giki.edu.pk',      '$2a$11$HEwWMONR0d2AuZP/hvWmo.Dt08ihnDhq0e16qMy6eIgUZNs/mL.JW','Student',1,'2024-03-02 09:15:00');
SET IDENTITY_INSERT dbo.Users OFF;
GO

INSERT INTO dbo.StudentProfiles (UserID, University, Degree, Semester, GPA, ExpectedGradYear, IsProfileComplete) VALUES
(17,'UET Lahore',            'BS Computer Science',       6,3.42,2025,1),
(18,'FAST NUCES Lahore',     'BS Software Engineering',   7,3.71,2025,1),
(19,'LUMS',                  'BS Data Science',           5,3.25,2026,1),
(20,'NUST Islamabad',        'BE Computer Engineering',   6,3.58,2025,1),
(21,'University of Punjab',  'BS Information Technology', 7,2.95,2025,1),
(22,'ITU Lahore',            'BS Artificial Intelligence',4,3.80,2027,1),
(23,'COMSATS Lahore',        'BS Computer Science',       8,3.10,2025,1),
(24,'GIK Institute',         'BE Software Engineering',   5,3.62,2026,1),
(25,'UET Lahore',            'BS Computer Science',       7,3.33,2025,1),
(26,'FAST NUCES Islamabad',  'BS Computer Science',       6,3.50,2026,1),
(27,'NUST Islamabad',        'BS Data Science',           5,3.88,2026,1),
(28,'ITU Lahore',            'BS Software Engineering',   6,3.45,2026,1),
(29,'LUMS',                  'BS Computer Science',       8,3.90,2024,1),
(30,'COMSATS Islamabad',     'BS Information Technology', 4,3.05,2027,1),
(31,'University of Punjab',  'BS Computer Science',       7,2.87,2025,1),
(32,'GIK Institute',         'BE Computer Engineering',   6,3.55,2026,1),
(33,'UET Lahore',            'BS Software Engineering',   5,3.30,2026,0),
(34,'FAST NUCES Lahore',     'BS Computer Science',       7,3.65,2025,1),
(35,'NUST Islamabad',        'BE Electrical Engineering', 6,3.20,2026,1),
(36,'ITU Lahore',            'BS Artificial Intelligence',3,3.95,2027,1),
(37,'COMSATS Lahore',        'BS Information Technology', 6,3.00,2026,0),
(38,'LUMS',                  'BS Computer Science',       7,3.75,2025,1),
(39,'GIK Institute',         'BE Software Engineering',   4,3.40,2027,1),
(40,'University of Punjab',  'BS Computer Science',       6,2.90,2026,1),
(41,'UET Lahore',            'BS Computer Science',       8,3.15,2025,1),
(42,'FAST NUCES Karachi',    'BS Software Engineering',   5,3.60,2026,1),
(43,'NUST Islamabad',        'BS Data Science',           7,3.48,2025,1),
(44,'COMSATS Abbottabad',    'BS Computer Science',       6,3.22,2026,1),
(45,'ITU Lahore',            'BS Artificial Intelligence',5,3.70,2026,1),
(46,'GIK Institute',         'BE Computer Engineering',   4,3.55,2027,1);
GO

INSERT INTO dbo.Companies (UserID, CompanyName, Industry, Website, Description, IsApproved, ApprovedAt) VALUES
(2,  'Arbisoft',                    'Software Services',   'https://arbisoft.com',          'Award-winning software product engineering company.',                          1,'2024-01-06 10:00:00'),
(3,  'Systems Limited',             'Technology',          'https://systemsltd.com',        'Enterprise technology solutions for Fortune 500 clients.',                    1,'2024-01-07 10:00:00'),
(4,  'NetSol Technologies',         'FinTech',             'https://netsoltech.com',        'Global IT services and FinTech solutions headquartered in Lahore.',           1,'2024-01-08 10:00:00'),
(5,  'TRG Pakistan',                'BPO / Technology',    'https://trg.com.pk',            'Technology and business process outsourcing group.',                          1,'2024-01-09 10:00:00'),
(6,  'Telenor Pakistan',            'Telecommunications',  'https://telenor.com.pk',        'One of Pakistan largest telecom operators.',                                  1,'2024-01-10 10:00:00'),
(7,  'Careem',                      'Ride-hailing / Tech', 'https://careem.com',            'Super app for the greater Middle East and South Asia.',                       1,'2024-01-11 10:00:00'),
(8,  'Avanza Solutions',            'Financial Technology', 'https://avanzasolutions.com',   'Digital banking and fintech solutions for the MENA region.',                  1,'2024-01-12 10:00:00'),
(9,  'Devsinc',                     'Software Services',   'https://devsinc.com',           'Product engineering company with teams across USA and Pakistan.',             1,'2024-01-13 10:00:00'),
(10, 'Techlogix',                   'Enterprise IT',       'https://techlogix.com',         'SAP and Oracle enterprise solutions provider.',                               1,'2024-01-14 10:00:00'),
(11, 'VentureDive',                 'Software Services',   'https://venturedive.com',       'Product and innovation lab building apps used by millions worldwide.',         1,'2024-01-15 10:00:00'),
(12, 'Folio3',                      'Software Services',   'https://folio3.com',            'Software engineering and AI company for US-based clients.',                   1,'2024-01-16 10:00:00'),
(13, 'Afiniti',                     'AI / Analytics',      'https://afiniti.com',           'AI-driven behavioral analytics company.',                                     1,'2024-01-17 10:00:00'),
(14, 'Inbox Business Technologies', 'Technology',          'https://inboxbusiness.com',     'Digital transformation delivering SAP, Oracle, and cloud solutions.',         1,'2024-01-18 10:00:00'),
(15, 'vteams',                      'Software Services',   'https://vteams.com',            'Dedicated remote software development teams for startups and enterprises.',   1,'2024-01-19 10:00:00'),
(16, 'Confiz',                      'Software Services',   'https://confiz.com',            'Data-driven digital agency building e-commerce and analytics solutions.',      0, NULL);
GO

SET IDENTITY_INSERT dbo.Skills ON;
INSERT INTO dbo.Skills (SkillID, SkillName, Category, IsActive) VALUES
(1,  'Python',          'Programming', 1),
(2,  'JavaScript',      'Programming', 1),
(3,  'TypeScript',      'Programming', 1),
(4,  'Java',            'Programming', 1),
(5,  'C#',              'Programming', 1),
(6,  'C++',             'Programming', 1),
(7,  'Go',              'Programming', 1),
(8,  'SQL',             'Database',    1),
(9,  'PostgreSQL',      'Database',    1),
(10, 'MySQL',           'Database',    1),
(11, 'MongoDB',         'Database',    1),
(12, 'Redis',           'Database',    1),
(13, 'React',           'Web',         1),
(14, 'Node.js',         'Web',         1),
(15, 'Django',          'Web',         1),
(16, 'Spring Boot',     'Web',         1),
(17, 'ASP.NET Core',    'Web',         1),
(18, 'REST API Design', 'Web',         1),
(19, 'Pandas',          'Data Science',1),
(20, 'NumPy',           'Data Science',1),
(21, 'Machine Learning','Data Science',1),
(22, 'TensorFlow',      'Data Science',1),
(23, 'Power BI',        'Data Science',1),
(24, 'Tableau',         'Data Science',1),
(25, 'Docker',          'DevOps',      1),
(26, 'Kubernetes',      'DevOps',      1),
(27, 'AWS',             'DevOps',      1),
(28, 'Azure',           'DevOps',      1),
(29, 'CI/CD',           'DevOps',      1),
(30, 'Figma',           'Design',      1);
SET IDENTITY_INSERT dbo.Skills OFF;
GO

INSERT INTO dbo.LearningResources (SkillID, Title, URL, Platform) VALUES
(1, 'Python for Everybody (Coursera)','https://www.coursera.org/specializations/python','Coursera'),
(1, 'Automate the Boring Stuff with Python','https://www.udemy.com/course/automate/','Udemy'),
(1, 'CS50P: Introduction to Python (Harvard)','https://cs50.harvard.edu/python/','edX'),
(2, 'The Complete JavaScript Course 2024','https://www.udemy.com/course/the-complete-javascript-course/','Udemy'),
(2, 'JavaScript Algorithms & Data Structures','https://www.freecodecamp.org/learn/javascript-algorithms-and-data-structures/','freeCodeCamp'),
(2, 'JavaScript Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=jS4aFq5-91M','YouTube'),
(3, 'TypeScript Full Course for Beginners','https://www.youtube.com/watch?v=30LWjhZzg50','YouTube'),
(3, 'Understanding TypeScript (Udemy)','https://www.udemy.com/course/understanding-typescript/','Udemy'),
(3, 'TypeScript Documentation Handbook','https://www.typescriptlang.org/docs/handbook/','Official Docs'),
(4, 'Java Programming Masterclass (Udemy)','https://www.udemy.com/course/java-the-complete-java-developer-course/','Udemy'),
(4, 'Java Full Course for Beginners (freeCodeCamp)','https://www.youtube.com/watch?v=A74TOX803D0','YouTube'),
(4, 'Java Programming Fundamentals (Coursera)','https://www.coursera.org/specializations/java-programming','Coursera'),
(5, 'C# Full Course – Beginners to Advanced','https://www.youtube.com/watch?v=GhQdlIFylQ8','YouTube'),
(5, 'C# Basics for Beginners (Udemy)','https://www.udemy.com/course/csharp-tutorial-for-beginners/','Udemy'),
(5, 'Microsoft C# Fundamentals','https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/','Official Docs'),
(6, 'C++ Tutorial for Beginners (freeCodeCamp)','https://www.youtube.com/watch?v=vLnPwxZdW4Y','YouTube'),
(6, 'Beginning C++ Programming (Udemy)','https://www.udemy.com/course/beginning-c-plus-plus-programming/','Udemy'),
(6, 'C++ Programming Course (Coursera)','https://www.coursera.org/specializations/coding-for-everyone','Coursera'),
(7, 'Go Programming Language Full Course','https://www.youtube.com/watch?v=un6ZyFkqFKo','YouTube'),
(7, 'Learn How To Code: Google Go (Udemy)','https://www.udemy.com/course/learn-how-to-code/','Udemy'),
(7, 'Go Tour – Official Interactive Tutorial','https://go.dev/tour/welcome/1','Official Docs'),
(8, 'SQL for Data Science (Coursera)','https://www.coursera.org/learn/sql-for-data-science','Coursera'),
(8, 'SQL Tutorial – Full Database Course (freeCodeCamp)','https://www.youtube.com/watch?v=HXV3zeQKqGY','YouTube'),
(8, 'The Complete SQL Bootcamp 2024 (Udemy)','https://www.udemy.com/course/the-complete-sql-bootcamp/','Udemy'),
(9, 'PostgreSQL Tutorial – Full Course','https://www.youtube.com/watch?v=qw--VYLpxG4','YouTube'),
(9, 'Learn PostgreSQL (Official Tutorial)','https://www.postgresql.org/docs/current/tutorial.html','Official Docs'),
(9, 'Databases and SQL for Data Science (Coursera)','https://www.coursera.org/learn/sql-data-science','Coursera'),
(10,'MySQL for Beginners (Udemy)','https://www.udemy.com/course/mysql-for-beginners/','Udemy'),
(10,'MySQL Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=HXV3zeQKqGY','YouTube'),
(10,'Introduction to Databases & SQL (Coursera)','https://www.coursera.org/learn/intro-sql','Coursera'),
(11,'MongoDB – The Complete Developers Guide (Udemy)','https://www.udemy.com/course/mongodb-the-complete-developers-guide/','Udemy'),
(11,'MongoDB Crash Course (Traversy Media)','https://www.youtube.com/watch?v=-56x56UppqQ','YouTube'),
(11,'MongoDB University Free Courses','https://university.mongodb.com/','Official Docs'),
(12,'Redis Crash Course (Traversy Media)','https://www.youtube.com/watch?v=jgpVdJB2sKQ','YouTube'),
(12,'Redis University: Introduction to Redis','https://university.redis.com/courses/ru101/','Official Docs'),
(12,'Learn Redis with Node.js (Udemy)','https://www.udemy.com/course/learn-redis/','Udemy'),
(13,'React – The Complete Guide 2024 (Udemy)','https://www.udemy.com/course/react-the-complete-guide-incl-redux/','Udemy'),
(13,'React Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=bMknfKXIFA8','YouTube'),
(13,'Front End Development Libraries (freeCodeCamp)','https://www.freecodecamp.org/learn/front-end-development-libraries/','freeCodeCamp'),
(14,'Node.js Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=Oe421EPjeBE','YouTube'),
(14,'The Complete Node.js Developer Course (Udemy)','https://www.udemy.com/course/the-complete-nodejs-developer-course-2/','Udemy'),
(14,'Node.js Back End Development (freeCodeCamp)','https://www.freecodecamp.org/learn/back-end-development-and-apis/','freeCodeCamp'),
(15,'Django for Everybody (Coursera)','https://www.coursera.org/specializations/django','Coursera'),
(15,'Python Django – The Practical Guide (Udemy)','https://www.udemy.com/course/python-django-the-practical-guide/','Udemy'),
(15,'Django Official Tutorial','https://docs.djangoproject.com/en/stable/intro/tutorial01/','Official Docs'),
(16,'Spring Boot Full Course (Amigoscode)','https://www.youtube.com/watch?v=9SGDpanrc8U','YouTube'),
(16,'Spring & Hibernate for Beginners (Udemy)','https://www.udemy.com/course/spring-hibernate-tutorial/','Udemy'),
(16,'Building Scalable Java Microservices (Coursera)','https://www.coursera.org/learn/google-cloud-java-spring','Coursera'),
(17,'ASP.NET Core Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=AhAxLiGC7Pc','YouTube'),
(17,'Complete ASP.NET Core MVC Course (Udemy)','https://www.udemy.com/course/complete-aspnet-core-21-course/','Udemy'),
(17,'ASP.NET Core Documentation (Microsoft Learn)','https://learn.microsoft.com/en-us/aspnet/core/','Official Docs'),
(18,'REST API Design Best Practices (YouTube)','https://www.youtube.com/watch?v=7nm1pYuKAhY','YouTube'),
(18,'Designing RESTful APIs (Udemy)','https://www.udemy.com/course/rest-api/','Udemy'),
(18,'API Design Fundamentals (Coursera)','https://www.coursera.org/learn/api-design-fundamentals','Coursera'),
(19,'Pandas for Data Analysis (Kaggle)','https://www.kaggle.com/learn/pandas','Kaggle'),
(19,'Data Analysis with Python (freeCodeCamp)','https://www.freecodecamp.org/learn/data-analysis-with-python/','freeCodeCamp'),
(19,'Python for Data Science – IBM (Coursera)','https://www.coursera.org/learn/python-for-applied-data-science-ai','Coursera'),
(20,'NumPy Crash Course (Traversy Media)','https://www.youtube.com/watch?v=QUT1VHiLmmI','YouTube'),
(20,'NumPy Tutorial (Kaggle)','https://www.kaggle.com/learn/intro-to-machine-learning','Kaggle'),
(20,'Applied Data Science with Python (Coursera)','https://www.coursera.org/specializations/data-science-python','Coursera'),
(21,'Machine Learning Specialization (Coursera – Andrew Ng)','https://www.coursera.org/specializations/machine-learning-introduction','Coursera'),
(21,'Intro to Machine Learning (Kaggle)','https://www.kaggle.com/learn/intro-to-machine-learning','Kaggle'),
(21,'Machine Learning Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=NWONeJKn6kc','YouTube'),
(22,'TensorFlow Developer Certificate (Coursera)','https://www.coursera.org/professional-certificates/tensorflow-in-practice','Coursera'),
(22,'Deep Learning with TensorFlow (Udemy)','https://www.udemy.com/course/tensorflow-2/','Udemy'),
(22,'TensorFlow Official Tutorials','https://www.tensorflow.org/tutorials','Official Docs'),
(23,'Microsoft Power BI Desktop (Coursera)','https://www.coursera.org/learn/power-bi','Coursera'),
(23,'Power BI Full Course (YouTube)','https://www.youtube.com/watch?v=3u7MQz1EyPY','YouTube'),
(23,'Microsoft Power BI Data Analyst (Microsoft Learn)','https://learn.microsoft.com/en-us/certifications/power-bi-data-analyst-associate/','Official Docs'),
(24,'Tableau for Beginners (Coursera – UC Davis)','https://www.coursera.org/learn/analytics-tableau','Coursera'),
(24,'Tableau Training for Beginners (YouTube)','https://www.youtube.com/watch?v=TPMlZxRRaBQ','YouTube'),
(24,'Tableau Desktop Specialist Prep (Udemy)','https://www.udemy.com/course/tableau10/','Udemy'),
(25,'Docker for Beginners Full Course (YouTube)','https://www.youtube.com/watch?v=fqMOX6JJhGo','YouTube'),
(25,'Docker & Kubernetes: The Practical Guide (Udemy)','https://www.udemy.com/course/docker-kubernetes-the-practical-guide/','Udemy'),
(25,'Docker Official Get Started Guide','https://docs.docker.com/get-started/','Official Docs'),
(26,'Kubernetes Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=X48VuDVv0do','YouTube'),
(26,'Kubernetes for Absolute Beginners (Udemy)','https://www.udemy.com/course/learn-kubernetes/','Udemy'),
(26,'Kubernetes Documentation Tutorial','https://kubernetes.io/docs/tutorials/kubernetes-basics/','Official Docs'),
(27,'AWS Certified Cloud Practitioner (freeCodeCamp)','https://www.youtube.com/watch?v=SOTamWNgDKc','YouTube'),
(27,'Ultimate AWS Certified Solutions Architect (Udemy)','https://www.udemy.com/course/aws-certified-solutions-architect-associate-saa-c03/','Udemy'),
(27,'AWS Cloud Practitioner Essentials (Coursera)','https://www.coursera.org/learn/aws-cloud-practitioner-essentials','Coursera'),
(28,'Microsoft Azure Fundamentals AZ-900 (Coursera)','https://www.coursera.org/learn/az-900-azure-fundamentals','Coursera'),
(28,'AZ-900 Azure Fundamentals Full Course (YouTube)','https://www.youtube.com/watch?v=5abffC-K40c','YouTube'),
(28,'Microsoft Azure Fundamentals (Microsoft Learn)','https://learn.microsoft.com/en-us/certifications/azure-fundamentals/','Official Docs'),
(29,'GitHub Actions Full Course (freeCodeCamp)','https://www.youtube.com/watch?v=R8_veQiYBjI','YouTube'),
(29,'CI/CD with Jenkins, Docker & Kubernetes (Udemy)','https://www.udemy.com/course/learn-devops-ci-cd-with-jenkins-using-pipelines-and-docker/','Udemy'),
(29,'DevOps Foundations: CI/CD (Coursera)','https://www.coursera.org/learn/devops-foundations-cicd','Coursera'),
(30,'Figma UI Design Tutorial: Get Started in 30 Minutes','https://www.youtube.com/watch?v=eZJOSK4E_yg','YouTube'),
(30,'UI/UX Design Specialization (Coursera – Google)','https://www.coursera.org/professional-certificates/google-ux-design','Coursera'),
(30,'Figma Essentials Course (Udemy)','https://www.udemy.com/course/figma-ux-ui-design-user-experience-tutorial-course/','Udemy');
GO

INSERT INTO dbo.StudentSkills (UserID, SkillID, ProficiencyLevel) VALUES
(17,2,'Advanced'),(17,3,'Intermediate'),(17,5,'Intermediate'),(17,8,'Intermediate'),(17,13,'Advanced'),(17,17,'Beginner'),
(18,1,'Advanced'),(18,19,'Advanced'),(18,20,'Intermediate'),(18,21,'Intermediate'),(18,22,'Beginner'),(18,23,'Intermediate'),
(19,4,'Intermediate'),(19,10,'Intermediate'),(19,25,'Intermediate'),(19,27,'Beginner'),(19,29,'Beginner'),
(20,1,'Intermediate'),(20,2,'Advanced'),(20,13,'Advanced'),(20,14,'Intermediate'),(20,8,'Advanced'),(20,11,'Intermediate'),
(21,4,'Advanced'),(21,16,'Intermediate'),(21,8,'Advanced'),(21,9,'Intermediate'),(21,25,'Beginner'),
(22,1,'Advanced'),(22,21,'Advanced'),(22,22,'Advanced'),(22,19,'Advanced'),(22,20,'Intermediate'),(22,24,'Beginner'),
(23,2,'Intermediate'),(23,13,'Intermediate'),(23,14,'Beginner'),(23,8,'Intermediate'),(23,30,'Beginner'),
(24,5,'Advanced'),(24,17,'Intermediate'),(24,8,'Advanced'),(24,12,'Beginner'),(24,25,'Beginner'),
(25,25,'Advanced'),(25,26,'Intermediate'),(25,27,'Advanced'),(25,29,'Advanced'),(25,8,'Intermediate'),
(26,30,'Advanced'),(26,2,'Intermediate'),(26,13,'Intermediate'),(26,3,'Beginner'),
(27,1,'Advanced'),(27,19,'Advanced'),(27,23,'Advanced'),(27,24,'Intermediate'),(27,8,'Advanced'),(27,21,'Intermediate'),
(28,2,'Advanced'),(28,3,'Advanced'),(28,13,'Advanced'),(28,14,'Intermediate'),(28,11,'Intermediate'),
(29,1,'Advanced'),(29,21,'Advanced'),(29,22,'Intermediate'),(29,27,'Advanced'),(29,28,'Intermediate'),(29,25,'Intermediate'),
(30,8,'Advanced'),(30,9,'Intermediate'),(30,11,'Beginner'),(30,1,'Beginner'),
(31,4,'Advanced'),(31,16,'Advanced'),(31,8,'Intermediate'),(31,25,'Beginner'),
(32,2,'Advanced'),(32,13,'Advanced'),(32,3,'Intermediate'),(32,30,'Intermediate'),
(33,6,'Advanced'),(33,4,'Intermediate'),(33,8,'Intermediate'),
(34,1,'Intermediate'),(34,19,'Intermediate'),(34,23,'Intermediate'),(34,8,'Intermediate'),
(35,6,'Advanced'),(35,5,'Intermediate'),(35,28,'Beginner'),
(36,1,'Advanced'),(36,22,'Advanced'),(36,21,'Advanced'),(36,20,'Advanced'),
(37,25,'Intermediate'),(37,27,'Intermediate'),(37,29,'Intermediate'),
(38,2,'Advanced'),(38,1,'Advanced'),(38,13,'Advanced'),(38,15,'Intermediate'),(38,8,'Advanced'),
(39,27,'Advanced'),(39,28,'Advanced'),(39,26,'Intermediate'),(39,25,'Intermediate'),(39,29,'Advanced'),
(40,23,'Intermediate'),(40,24,'Beginner'),(40,8,'Intermediate'),
(41,5,'Advanced'),(41,17,'Advanced'),(41,8,'Advanced'),(41,9,'Intermediate'),(41,25,'Intermediate'),
(42,30,'Advanced'),(42,2,'Intermediate'),(42,13,'Beginner'),
(43,1,'Advanced'),(43,8,'Advanced'),(43,9,'Intermediate'),(43,19,'Advanced'),(43,27,'Intermediate'),
(44,7,'Intermediate'),(44,8,'Intermediate'),(44,25,'Beginner'),
(45,1,'Advanced'),(45,21,'Advanced'),(45,22,'Intermediate'),(45,7,'Intermediate'),
(46,2,'Intermediate'),(46,13,'Intermediate'),(46,30,'Intermediate');
GO

SET IDENTITY_INSERT dbo.Jobs ON;
INSERT INTO dbo.Jobs (JobID, CompanyID, Title, Description, JobType, Duration, Deadline, Status, PostedAt) VALUES
(1,  2,  'Software Engineer Intern',         'Work on backend microservices using C# and ASP.NET Core. Assist in REST API development and unit testing.',                      'Internship','3 Months', '2025-07-31','Active','2025-05-01 09:00:00'),
(2,  2,  'Backend Developer',                'Design and maintain scalable REST APIs using Node.js and PostgreSQL. Implement CI/CD pipelines on AWS.',                          'Full-time', 'Permanent','2025-06-30','Active','2025-05-02 10:00:00'),
(3,  3,  'Frontend Developer',               'Build high-performance React applications with TypeScript. Integrate RESTful APIs and ensure responsive UI components.',            'Full-time', 'Permanent','2025-07-15','Active','2025-05-03 10:30:00'),
(4,  3,  'Data Analyst Intern',              'Analyze business datasets using Python and Power BI. Create dashboards and write SQL queries to extract KPIs.',                    'Internship','6 Months', '2025-08-01','Active','2025-05-04 11:00:00'),
(5,  4,  'DevOps Engineer',                  'Maintain Docker-based microservices on AWS EKS. Build CI/CD pipelines using GitHub Actions and Jenkins.',                          'Full-time', 'Permanent','2025-06-15','Active','2025-05-05 09:00:00'),
(6,  4,  'AI/ML Engineer',                   'Develop and deploy machine learning models using Python, TensorFlow, and AWS SageMaker.',                                          'Full-time', 'Permanent','2025-07-01','Active','2025-05-06 09:30:00'),
(7,  5,  'Java Backend Intern',              'Build RESTful web services using Spring Boot and MySQL. Write unit tests and participate in code reviews.',                         'Internship','3 Months', '2025-07-20','Active','2025-05-07 10:00:00'),
(8,  6,  'Data Engineer',                    'Design data pipelines using Python and SQL. Work with AWS Redshift and Glue. Optimize ETL processes.',                             'Full-time', 'Permanent','2025-06-28','Active','2025-05-08 10:00:00'),
(9,  6,  'Cloud Solutions Architect Intern', 'Support cloud architecture design on AWS and Azure. Document solution blueprints.',                                                'Internship','6 Months', '2025-08-15','Active','2025-05-09 11:00:00'),
(10, 7,  'Full Stack Developer',             'Build and maintain features for the Careem super-app using React and Node.js with TypeScript.',                                    'Full-time', 'Permanent','2025-07-10','Active','2025-05-10 09:00:00'),
(11, 7,  'Mobile Backend Engineer',          'Develop REST APIs for mobile clients using Node.js and MongoDB. Implement Redis caching.',                                         'Full-time', 'Permanent','2025-06-20','Active','2025-05-11 10:00:00'),
(12, 8,  '.NET Core Developer',              'Design enterprise banking APIs using ASP.NET Core and SQL Server. Implement OAuth 2.0 and JWT security patterns.',                 'Full-time', 'Permanent','2025-07-05','Active','2025-05-12 10:30:00'),
(13, 8,  'Database Administrator Intern',    'Assist in designing and maintaining SQL Server and PostgreSQL databases. Write stored procedures and optimize queries.',            'Internship','3 Months', '2025-07-25','Active','2025-05-13 11:00:00'),
(14, 9,  'React Developer',                  'Develop React web applications with TypeScript and Tailwind CSS. Translate Figma prototypes to pixel-perfect UIs.',                'Full-time', 'Permanent','2025-07-01','Active','2025-05-14 09:00:00'),
(15, 9,  'UI/UX Designer Intern',            'Create wireframes and prototypes in Figma. Conduct user research and usability testing.',                                          'Internship','4 Months', '2025-08-10','Active','2025-05-15 09:30:00'),
(16, 10, 'SAP ABAP Developer',               'Develop ABAP programs and BAPI enhancements for SAP ERP systems.',                                                                 'Full-time', 'Permanent','2025-06-25','Active','2025-05-16 10:00:00'),
(17, 10, 'Python Automation Intern',         'Automate business workflows using Python scripts and REST APIs.',                                                                  'Internship','3 Months', '2025-07-30','Active','2025-05-17 10:00:00'),
(18, 11, 'Product Engineer – Backend',       'Build scalable backend services using Go and PostgreSQL. Design RESTful APIs and write integration tests.',                        'Full-time', 'Permanent','2025-07-08','Active','2025-05-18 09:00:00'),
(19, 11, 'Machine Learning Intern',          'Implement ML models using Python, scikit-learn, and TensorFlow. Analyse datasets and report model performance.',                   'Internship','6 Months', '2025-09-01','Active','2025-05-19 09:30:00'),
(20, 12, 'Node.js Developer',                'Build REST APIs in Node.js and MongoDB. Integrate third-party services and maintain API documentation.',                           'Full-time', 'Permanent','2025-07-12','Active','2025-05-20 10:00:00'),
(21, 12, 'Data Science Intern',              'Apply Pandas and NumPy to clean real-world datasets. Build predictive models and present findings using Tableau.',                 'Internship','4 Months', '2025-08-20','Active','2025-05-21 10:30:00'),
(22, 13, 'AI Research Engineer',             'Research and prototype AI models for behavioral analytics using Python and TensorFlow.',                                           'Full-time', 'Permanent','2025-07-20','Active','2025-05-22 09:00:00'),
(23, 14, 'Azure Cloud Engineer',             'Deploy and manage workloads on Microsoft Azure. Configure Azure DevOps pipelines and Kubernetes clusters.',                        'Full-time', 'Permanent','2025-06-30','Active','2025-05-23 09:30:00'),
(24, 15, 'Full Stack Intern',                'Build full-stack features using React, Node.js, and MongoDB in an agile startup environment.',                                     'Internship','3 Months', '2025-08-05','Active','2025-05-24 10:00:00'),
(25, 15, 'Junior Software Developer',        'Develop and maintain web applications using JavaScript, React, and Node.js. Write clean, testable code.',                          'Full-time', 'Permanent','2025-07-25','Active','2025-05-25 10:30:00');
SET IDENTITY_INSERT dbo.Jobs OFF;
GO

INSERT INTO dbo.JobSkills (JobID, SkillID, Priority) VALUES
(1,5,'Required'),(1,17,'Required'),(1,8,'Preferred'),(1,25,'Bonus'),
(2,14,'Required'),(2,9,'Required'),(2,27,'Preferred'),(2,29,'Preferred'),
(3,13,'Required'),(3,3,'Required'),(3,2,'Preferred'),(3,30,'Bonus'),
(4,1,'Required'),(4,23,'Required'),(4,8,'Required'),(4,19,'Preferred'),
(5,25,'Required'),(5,26,'Required'),(5,27,'Required'),(5,29,'Required'),
(6,1,'Required'),(6,22,'Required'),(6,21,'Required'),(6,27,'Preferred'),
(7,4,'Required'),(7,16,'Required'),(7,10,'Preferred'),(7,8,'Preferred'),
(8,1,'Required'),(8,8,'Required'),(8,27,'Required'),(8,19,'Preferred'),
(9,27,'Required'),(9,28,'Preferred'),(9,25,'Bonus'),
(10,13,'Required'),(10,14,'Required'),(10,3,'Required'),(10,11,'Preferred'),
(11,14,'Required'),(11,11,'Required'),(11,12,'Preferred'),(11,8,'Bonus'),
(12,5,'Required'),(12,17,'Required'),(12,8,'Required'),(12,12,'Preferred'),
(13,8,'Required'),(13,9,'Required'),(13,10,'Preferred'),
(14,13,'Required'),(14,3,'Required'),(14,30,'Preferred'),(14,2,'Bonus'),
(15,30,'Required'),(15,2,'Preferred'),(15,13,'Bonus'),
(16,4,'Required'),(16,8,'Required'),(16,6,'Preferred'),
(17,1,'Required'),(17,8,'Preferred'),(17,19,'Bonus'),
(18,7,'Required'),(18,9,'Required'),(18,18,'Required'),(18,25,'Preferred'),
(19,1,'Required'),(19,22,'Preferred'),(19,21,'Required'),(19,20,'Bonus'),
(20,14,'Required'),(20,11,'Required'),(20,2,'Preferred'),(20,18,'Preferred'),
(21,1,'Required'),(21,19,'Required'),(21,20,'Preferred'),(21,24,'Bonus'),
(22,1,'Required'),(22,22,'Required'),(22,21,'Required'),(22,20,'Preferred'),
(23,28,'Required'),(23,26,'Required'),(23,25,'Preferred'),(23,29,'Preferred'),
(24,13,'Required'),(24,14,'Required'),(24,11,'Preferred'),(24,2,'Bonus'),
(25,2,'Required'),(25,13,'Required'),(25,14,'Preferred'),(25,3,'Preferred');
GO

INSERT INTO dbo.Applications (JobID, UserID, MatchScore, AppliedAt, CurrentStatus) VALUES
(1,17,78.50,'2025-05-03 10:00:00','Shortlisted'),
(1,24,91.25,'2025-05-03 12:00:00','Interview'),
(1,41,95.00,'2025-05-04 09:00:00','Hired'),
(2,20,87.50,'2025-05-05 10:00:00','Shortlisted'),
(2,25,82.00,'2025-05-06 11:00:00','Applied'),
(3,17,75.00,'2025-05-06 09:00:00','Applied'),
(3,28,92.50,'2025-05-07 10:00:00','Interview'),
(3,32,88.00,'2025-05-07 11:00:00','Shortlisted'),
(3,26,70.00,'2025-05-08 12:00:00','Applied'),
(4,18,93.75,'2025-05-08 09:00:00','Hired'),
(4,27,95.00,'2025-05-08 10:00:00','Interview'),
(4,34,72.00,'2025-05-09 09:00:00','Applied'),
(4,43,80.50,'2025-05-09 10:00:00','Shortlisted'),
(5,19,78.00,'2025-05-09 11:00:00','Applied'),
(5,25,97.50,'2025-05-10 09:00:00','Hired'),
(5,37,71.25,'2025-05-10 10:00:00','Applied'),
(5,39,91.00,'2025-05-10 11:00:00','Shortlisted'),
(6,22,96.25,'2025-05-11 09:00:00','Interview'),
(6,29,89.00,'2025-05-11 10:00:00','Shortlisted'),
(6,36,92.50,'2025-05-12 09:00:00','Hired'),
(6,45,88.00,'2025-05-12 10:00:00','Interview'),
(7,21,92.00,'2025-05-13 09:00:00','Shortlisted'),
(7,31,94.50,'2025-05-13 10:00:00','Interview'),
(7,33,60.00,'2025-05-13 11:00:00','Applied'),
(8,19,65.00,'2025-05-14 09:00:00','Applied'),
(8,27,85.00,'2025-05-14 10:00:00','Shortlisted'),
(8,43,90.00,'2025-05-15 09:00:00','Interview'),
(9,39,88.33,'2025-05-15 10:00:00','Interview'),
(9,29,83.33,'2025-05-15 11:00:00','Shortlisted'),
(10,20,87.50,'2025-05-16 09:00:00','Applied'),
(10,28,93.75,'2025-05-16 10:00:00','Interview'),
(10,38,91.25,'2025-05-16 11:00:00','Shortlisted'),
(11,20,83.33,'2025-05-17 09:00:00','Applied'),
(11,28,91.67,'2025-05-17 10:00:00','Shortlisted'),
(12,24,88.75,'2025-05-18 09:00:00','Shortlisted'),
(12,41,93.50,'2025-05-18 10:00:00','Interview'),
(13,30,83.33,'2025-05-19 09:00:00','Applied'),
(13,41,88.89,'2025-05-19 10:00:00','Shortlisted'),
(13,43,91.67,'2025-05-19 11:00:00','Interview'),
(14,26,83.33,'2025-05-20 09:00:00','Applied'),
(14,28,91.67,'2025-05-20 10:00:00','Shortlisted'),
(14,32,87.50,'2025-05-20 11:00:00','Applied'),
(15,26,94.44,'2025-05-21 09:00:00','Hired'),
(15,42,83.33,'2025-05-21 10:00:00','Shortlisted'),
(15,46,77.78,'2025-05-21 11:00:00','Applied'),
(17,18,88.89,'2025-05-22 09:00:00','Applied'),
(17,34,77.78,'2025-05-22 10:00:00','Applied'),
(18,44,87.50,'2025-05-23 09:00:00','Shortlisted'),
(18,45,75.00,'2025-05-23 10:00:00','Applied'),
(19,22,91.67,'2025-05-24 09:00:00','Shortlisted'),
(19,36,100.0,'2025-05-24 10:00:00','Interview'),
(21,22,83.33,'2025-05-25 09:00:00','Applied'),
(21,27,87.50,'2025-05-25 10:00:00','Shortlisted'),
(22,29,93.75,'2025-05-26 09:00:00','Interview'),
(22,36,100.0,'2025-05-26 10:00:00','Hired'),
(22,45,87.50,'2025-05-26 11:00:00','Applied'),
(23,39,93.75,'2025-05-27 09:00:00','Interview'),
(23,29,87.50,'2025-05-27 10:00:00','Shortlisted'),
(24,20,87.50,'2025-05-28 09:00:00','Applied'),
(24,28,91.67,'2025-05-28 10:00:00','Shortlisted'),
(25,17,87.50,'2025-05-29 09:00:00','Applied'),
(25,28,93.75,'2025-05-29 10:00:00','Shortlisted'),
(25,38,91.25,'2025-05-29 11:00:00','Applied');
GO

-- Status log: initial Applied entry for every application
INSERT INTO dbo.ApplicationStatusLog (ApplicationID, Status, ChangedAt, ChangedByUserID)
SELECT ApplicationID, 'Applied', AppliedAt, UserID FROM dbo.Applications;

-- Shortlisted transitions
INSERT INTO dbo.ApplicationStatusLog (ApplicationID, Status, ChangedAt, ChangedByUserID)
SELECT a.ApplicationID, 'Shortlisted', DATEADD(DAY,2,a.AppliedAt),
       (SELECT TOP 1 c.UserID FROM Jobs j JOIN Companies c ON j.CompanyID=c.CompanyID WHERE j.JobID=a.JobID)
FROM dbo.Applications a WHERE a.CurrentStatus IN ('Shortlisted','Interview','Hired');

-- Interview transitions
INSERT INTO dbo.ApplicationStatusLog (ApplicationID, Status, ChangedAt, ChangedByUserID)
SELECT a.ApplicationID, 'Interview', DATEADD(DAY,5,a.AppliedAt),
       (SELECT TOP 1 c.UserID FROM Jobs j JOIN Companies c ON j.CompanyID=c.CompanyID WHERE j.JobID=a.JobID)
FROM dbo.Applications a WHERE a.CurrentStatus IN ('Interview','Hired');

-- Hired transitions
INSERT INTO dbo.ApplicationStatusLog (ApplicationID, Status, ChangedAt, ChangedByUserID)
SELECT a.ApplicationID, 'Hired', DATEADD(DAY,10,a.AppliedAt),
       (SELECT TOP 1 c.UserID FROM Jobs j JOIN Companies c ON j.CompanyID=c.CompanyID WHERE j.JobID=a.JobID)
FROM dbo.Applications a WHERE a.CurrentStatus = 'Hired';
GO

INSERT INTO dbo.ApplicationNotes (ApplicationID, NoteText, CreatedAt)
SELECT ApplicationID,
       CASE CurrentStatus
           WHEN 'Applied'     THEN CONCAT('Application received. CV forwarded to hiring manager.')
           WHEN 'Shortlisted' THEN CONCAT('Candidate shortlisted. Match score: ', MatchScore, '%.')
           WHEN 'Interview'   THEN CONCAT('Interview scheduled. Match score: ', MatchScore, '%.')
           WHEN 'Hired'       THEN CONCAT('Offer extended and accepted. Match score: ', MatchScore, '%.')
           ELSE CONCAT('Status updated to ', CurrentStatus, '.')
       END,
       DATEADD(HOUR,2,AppliedAt)
FROM dbo.Applications;
GO

INSERT INTO dbo.Alerts (UserID, Message, AlertType, IsRead, CreatedAt) VALUES
(17,'Your application for Software Engineer Intern at Arbisoft has been shortlisted!','StatusChange',0,'2025-05-05 10:00:00'),
(17,'New job match: Frontend Developer at Systems Limited – 75% match.','NewJob',0,'2025-05-06 08:00:00'),
(18,'Congratulations! You have been hired as Data Analyst Intern at Systems Limited.','StatusChange',0,'2025-05-18 10:00:00'),
(18,'New high-match internship: Python Automation Intern at Techlogix – 89% match.','NewJob',0,'2025-05-18 11:00:00'),
(19,'Your application for DevOps Engineer at NetSol Technologies is under review.','StatusChange',1,'2025-05-10 09:00:00'),
(19,'Tip: Add Azure certification to boost your Cloud Engineer match scores.','MatchUpdate',0,'2025-05-11 08:00:00'),
(20,'Your application for Backend Developer at Arbisoft has been shortlisted.','StatusChange',0,'2025-05-07 10:00:00'),
(20,'New job match: Full Stack Developer at Careem – 87% match.','NewJob',0,'2025-05-16 08:00:00'),
(21,'Java Backend Intern at TRG – your application is shortlisted!','StatusChange',0,'2025-05-15 10:00:00'),
(22,'Interview scheduled for AI/ML Engineer at NetSol Technologies.','Interview',0,'2025-05-16 10:00:00'),
(22,'Your ML Intern application at VentureDive has been shortlisted.','StatusChange',0,'2025-05-25 10:00:00'),
(23,'New job matching your skills: React Developer at Devsinc – 71% match.','NewJob',0,'2025-05-14 08:00:00'),
(24,'.NET Core Developer at Avanza – shortlisted! Expect a call within 3 days.','StatusChange',0,'2025-05-20 10:00:00'),
(24,'Technical interview scheduled for Software Engineer Intern at Arbisoft.','Interview',0,'2025-05-09 10:00:00'),
(25,'Congratulations! You are hired as DevOps Engineer at NetSol Technologies.','StatusChange',0,'2025-05-20 09:00:00'),
(26,'Congratulations! You are hired as UI/UX Designer Intern at Devsinc.','StatusChange',0,'2025-05-31 09:00:00'),
(27,'Data Analyst Intern at Systems Ltd – you are in the interview stage!','Interview',0,'2025-05-13 10:00:00'),
(28,'Frontend Developer at Systems Ltd – interview scheduled.','Interview',0,'2025-05-12 10:00:00'),
(29,'Cloud Solutions Architect Intern at Telenor – shortlisted.','StatusChange',0,'2025-05-17 10:00:00'),
(30,'DBA Intern at Avanza – application received and under review.','StatusChange',1,'2025-05-19 09:00:00'),
(31,'Java Backend Intern at TRG – technical interview scheduled.','Interview',0,'2025-05-18 10:00:00'),
(32,'Frontend Developer at Systems Ltd – shortlisted!','StatusChange',0,'2025-05-09 10:00:00'),
(34,'Python Automation Intern at Techlogix – application received.','StatusChange',0,'2025-05-22 09:00:00'),
(36,'Congratulations! You are hired as AI Research Engineer at Afiniti.','StatusChange',0,'2025-06-05 10:00:00'),
(36,'ML Intern at VentureDive – interview stage reached.','Interview',0,'2025-05-29 10:00:00'),
(38,'Junior Software Developer at vteams – application received.','StatusChange',0,'2025-05-29 09:00:00'),
(39,'Azure Cloud Engineer at Inbox – interview stage reached!','Interview',0,'2025-06-01 09:00:00'),
(41,'Congratulations! You are hired as Software Engineer Intern at Arbisoft.','StatusChange',0,'2025-05-14 09:00:00'),
(41,'.NET Core Developer at Avanza – technical interview scheduled.','Interview',0,'2025-05-23 10:00:00'),
(42,'UI/UX Designer Intern at Devsinc – shortlisted.','StatusChange',0,'2025-05-25 10:00:00'),
(43,'Data Engineer at Telenor – interview stage reached.','Interview',0,'2025-05-20 10:00:00'),
(44,'Product Engineer (Backend) at VentureDive – shortlisted.','StatusChange',0,'2025-05-26 09:00:00'),
(45,'AI Research Engineer at Afiniti – application received.','StatusChange',0,'2025-05-26 09:00:00'),
(46,'UI/UX Designer Intern at Devsinc – application received.','StatusChange',1,'2025-05-21 09:00:00');
GO

/* ── 7. VERIFICATION ─────────────────────────────────────── */
SELECT 'Users'                AS TableName, COUNT(*) AS Rows FROM dbo.Users
UNION ALL SELECT 'StudentProfiles',  COUNT(*) FROM dbo.StudentProfiles
UNION ALL SELECT 'Companies',        COUNT(*) FROM dbo.Companies
UNION ALL SELECT 'Skills',           COUNT(*) FROM dbo.Skills
UNION ALL SELECT 'LearningResources',COUNT(*) FROM dbo.LearningResources
UNION ALL SELECT 'StudentSkills',    COUNT(*) FROM dbo.StudentSkills
UNION ALL SELECT 'Jobs',             COUNT(*) FROM dbo.Jobs
UNION ALL SELECT 'JobSkills',        COUNT(*) FROM dbo.JobSkills
UNION ALL SELECT 'Applications',     COUNT(*) FROM dbo.Applications
UNION ALL SELECT 'StatusLog',        COUNT(*) FROM dbo.ApplicationStatusLog
UNION ALL SELECT 'AppNotes',         COUNT(*) FROM dbo.ApplicationNotes
UNION ALL SELECT 'Alerts',           COUNT(*) FROM dbo.Alerts
ORDER BY TableName;
GO