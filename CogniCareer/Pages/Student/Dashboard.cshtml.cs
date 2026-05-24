using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;
using CogniCareer.Models;
using CogniCareer.Helpers;
using System.Text.Json;

namespace CogniCareer.Pages.Student
{
    public class DashboardModel : PageModel
    {
        private readonly AuthService _auth;
        private readonly StudentService _studentService;
        private readonly JobService _jobService;
        private readonly ApplicationService _appService;
        private readonly AlertService _alertService;
        private readonly MatchScoreService _matchService;
        private readonly AdminService _adminService;
        private readonly AIService _aiService;   // NEW

        public DashboardModel(AuthService auth, StudentService studentService, JobService jobService,
            ApplicationService appService, AlertService alertService,
            MatchScoreService matchService, AdminService adminService,
            AIService aiService)                  // NEW
        {
            _auth = auth; _studentService = studentService; _jobService = jobService;
            _appService = appService; _alertService = alertService;
            _matchService = matchService; _adminService = adminService;
            _aiService = aiService;               // NEW
        }

        public string UserName { get; set; } = "";
        public string Initials { get; set; } = "";
        public bool ProfileComplete { get; set; }
        public StudentProfile? Profile { get; set; }
        public List<Application> Applications { get; set; } = new();
        public List<Application> RecentApplications { get; set; } = new();
        public List<JobWithMatchScore> AllJobs { get; set; } = new();
        public List<JobWithMatchScore> TopMatchedJobs { get; set; } = new();
        public List<StudentSkill> StudentSkills { get; set; } = new();
        public List<Alert> Alerts { get; set; } = new();
        public SkillGapResult TopGap { get; set; } = new();
        public List<LearningResource> LearningResources { get; set; } = new();
        public PeerBenchmark TopBenchmark { get; set; } = new();
        public HashSet<int> AppliedJobIds { get; set; } = new();
        public string AllSkillsJson { get; set; } = "[]";
        public string ProfileMessage { get; set; } = "";
        public string ToastMessage { get; set; } = "";
        public string ToastClass { get; set; } = "t-lime";
        public int TotalApplications => Applications.Count;
        public int PendingCount => Applications.Count(a => a.CurrentStatus == "Applied");
        public int ShortlistedCount => Applications.Count(a => a.CurrentStatus == "Shortlisted");
        public int InterviewCount => Applications.Count(a => a.CurrentStatus == "Interview");
        public int RejectedCount => Applications.Count(a => a.CurrentStatus == "Rejected");
        public int TotalSkills => StudentSkills.Count;
        public decimal AvgMatchScore => Applications.Any() ? Math.Round(Applications.Average(a => a.MatchScore), 1) : 0;
        public int UnreadAlerts { get; set; }

        public IActionResult OnGet()
        {
            if (!_auth.IsStudent()) return RedirectToPage("/Auth/StudentAuth");
            LoadData();
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            _auth.Logout();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostApply(int jobId, decimal matchScore)
        {
            if (!_auth.IsStudent()) return RedirectToPage("/Auth/StudentAuth");
            var uid = _auth.GetUserID()!.Value;
            var (success, msg) = _appService.Apply(uid, jobId, matchScore);
            TempData["Toast"] = msg;
            TempData["ToastClass"] = success ? "t-lime" : "t-red";
            return RedirectToPage();
        }

        public IActionResult OnPostSaveProfile(string University, string Degree, int Semester, decimal GPA, int ExpectedGradYear)
        {
            if (!_auth.IsStudent()) return RedirectToPage("/Auth/StudentAuth");
            var uid = _auth.GetUserID()!.Value;
            var profile = new StudentProfile
            {
                UserID = uid,
                University = University,
                Degree = Degree,
                Semester = Semester,
                GPA = GPA,
                ExpectedGradYear = ExpectedGradYear,
                IsProfileComplete = true
            };
            _studentService.SaveProfile(profile);
            TempData["Toast"] = "Profile saved successfully!";
            TempData["ToastClass"] = "t-lime";
            return RedirectToPage();
        }

        public IActionResult OnPostAddSkill(int skillId, string proficiency)
        {
            if (!_auth.IsStudent()) return RedirectToPage("/Auth/StudentAuth");
            if (skillId <= 0)
            {
                TempData["Toast"] = "Please select a skill from the search list.";
                TempData["ToastClass"] = "t-red";
                return RedirectToPage();
            }
            var uid = _auth.GetUserID()!.Value;
            var skillData = new CogniCareer.Data.StudentSkillData();
            if (!skillData.HasSkill(uid, skillId))
            {
                skillData.AddStudentSkill(new StudentSkill { UserID = uid, SkillID = skillId, ProficiencyLevel = proficiency });
                TempData["Toast"] = "Skill added!";
                TempData["ToastClass"] = "t-lime";
            }
            else
            {
                TempData["Toast"] = "You already have that skill.";
                TempData["ToastClass"] = "t-red";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteSkill(int studentSkillId)
        {
            if (!_auth.IsStudent()) return RedirectToPage("/Auth/StudentAuth");
            var skillData = new CogniCareer.Data.StudentSkillData();
            skillData.DeleteStudentSkill(studentSkillId);
            TempData["Toast"] = "Skill removed.";
            TempData["ToastClass"] = "t-red";
            return RedirectToPage();
        }

        public IActionResult OnPostMarkRead(int alertId)
        {
            if (!_auth.IsStudent()) return RedirectToPage("/Auth/StudentAuth");
            _alertService.MarkRead(alertId);
            return RedirectToPage();
        }

        public IActionResult OnPostMarkAllRead()
        {
            if (!_auth.IsStudent()) return RedirectToPage("/Auth/StudentAuth");
            _alertService.MarkAllRead(_auth.GetUserID()!.Value);
            return RedirectToPage();
        }

        // ─────────────────────────────────────────────────────────
        //  NEW: AJAX handler — AI Resume Analyzer
        //  Called from JS via fetch() with JSON body.
        //  [IgnoreAntiforgeryToken] keeps the AJAX call simple for this project.
        // ─────────────────────────────────────────────────────────
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAnalyzeResumeAsync([FromBody] ResumeRequest req)
        {
            if (!_auth.IsStudent())
                return new JsonResult(new { success = false, errorMessage = "Not signed in." });

            var uid = _auth.GetUserID()!.Value;
            var profile = _studentService.GetProfile(uid);
            var skills = new CogniCareer.Data.StudentSkillData().GetByUserID(uid);
            var activeJobs = _jobService.GetAllActiveJobs();
            var topJobs = _matchService.GetRankedJobs(uid, activeJobs).Take(5).ToList();

            var result = await _aiService.AnalyzeResumeAsync(req?.Text ?? "", profile, skills, topJobs);
            return new JsonResult(result);
        }

        // ─────────────────────────────────────────────────────────
        //  NEW: AJAX handler — AI Career Advisor (chat)
        // ─────────────────────────────────────────────────────────
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAskAdvisorAsync([FromBody] AdvisorRequest req)
        {
            if (!_auth.IsStudent())
                return new JsonResult(new { reply = "Please sign in again." });

            var uid = _auth.GetUserID()!.Value;
            var profile = _studentService.GetProfile(uid);
            var skills = new CogniCareer.Data.StudentSkillData().GetByUserID(uid);
            var apps = _appService.GetByUser(uid);
            var activeJobs = _jobService.GetAllActiveJobs();
            var topJobs = _matchService.GetRankedJobs(uid, activeJobs).Take(5).ToList();

            SkillGapResult? gap = null;
            if (apps.Any())
            {
                var topApp = apps.OrderByDescending(a => a.MatchScore).First();
                gap = _matchService.GetGap(uid, topApp.JobID);
            }

            string reply = await _aiService.AskAdvisorAsync(
                req?.Question ?? "",
                req?.History ?? new List<ChatMessage>(),
                profile, skills, topJobs, gap);

            return new JsonResult(new { reply });
        }

        // DTOs for the two AJAX handlers above
        public class ResumeRequest
        {
            public string Text { get; set; } = "";
        }

        public class AdvisorRequest
        {
            public string Question { get; set; } = "";
            public List<ChatMessage> History { get; set; } = new();
        }

        private void LoadData()
        {
            var uid = _auth.GetUserID()!.Value;
            UserName = _auth.GetUserName();
            Initials = SessionHelper.GetInitials(UserName);
            Profile = _studentService.GetProfile(uid);
            ProfileComplete = Profile?.IsProfileComplete ?? false;

            Applications = _appService.GetByUser(uid);
            RecentApplications = Applications.OrderByDescending(a => a.AppliedAt).Take(5).ToList();
            AppliedJobIds = Applications.Select(a => a.JobID).ToHashSet();

            StudentSkills = new CogniCareer.Data.StudentSkillData().GetByUserID(uid);
            var activeJobs = _jobService.GetAllActiveJobs();
            AllJobs = _matchService.GetRankedJobs(uid, activeJobs);
            TopMatchedJobs = AllJobs.Take(5).ToList();

            if (Applications.Any())
            {
                var topApp = Applications.OrderByDescending(a => a.MatchScore).First();
                TopGap = _matchService.GetGap(uid, topApp.JobID);
                TopBenchmark = _appService.GetBenchmark(uid, topApp.JobID);
                if (TopGap.MissingSkills.Any())
                {
                    var missingSkillIDs = TopGap.MissingSkills.Select(s => s.SkillID).ToList();
                    LearningResources = new CogniCareer.Data.LearningResourceData().GetBySkillIDs(missingSkillIDs);
                }
            }

            Alerts = _alertService.GetUnread(uid);
            UnreadAlerts = Alerts.Count;

            var allSkillsList = _adminService.GetActiveSkills().Select(s => new { skillID = s.SkillID, skillName = s.SkillName, category = s.Category }).ToList();
            AllSkillsJson = JsonSerializer.Serialize(allSkillsList);

            if (TempData.ContainsKey("Toast"))
            {
                ToastMessage = TempData["Toast"]?.ToString() ?? "";
                ToastClass = TempData["ToastClass"]?.ToString() ?? "t-lime";
            }
        }
    }
}