using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;
using CogniCareer.Models;
using CogniCareer.Helpers;
using System.Text.Json;
using System.Text;
using UglyToad.PdfPig;

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
        public List<ChatMessage> ChatHistory { get; set; } = new();
        public string ChatHistoryJson { get; set; } = "[]";
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

            // === NEW: log AI usage so it appears in admin analytics ===
            if (result.Success)
            {
                new CogniCareer.Data.ActivityData().Log(
                    "ai_resume",
                    "AI Resume Analyzed",
                    $"{profile?.FullName ?? _auth.GetUserName()} scored {result.OverallScore}/100 on the AI Resume Analyzer.");
            }

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
            List<LearningResource> learningResources = new();
            if (apps.Any())
            {
                var topApp = apps.OrderByDescending(a => a.MatchScore).First();
                gap = _matchService.GetGap(uid, topApp.JobID);
                if (gap.MissingSkills.Any())
                {
                    var missingIds = gap.MissingSkills.Select(s => s.SkillID).ToList();
                    learningResources = new CogniCareer.Data.LearningResourceData().GetBySkillIDs(missingIds);
                }
            }

            string question = req?.Question ?? "";
            string reply = await _aiService.AskAdvisorAsync(
                question,
                req?.History ?? new List<ChatMessage>(),
                profile, skills, topJobs, gap, learningResources);

            // === NEW: persist the new turn (user question + AI reply) to DB ===
            var chatData = new CogniCareer.Data.AIChatData();
            chatData.Add(uid, "user", question);
            chatData.Add(uid, "model", reply);

            // Activity log
            var preview = question.Length > 80 ? (question.Substring(0, 80) + "...") : question;
            new CogniCareer.Data.ActivityData().Log(
                "ai_advisor",
                "AI Advisor Question",
                $"{profile?.FullName ?? _auth.GetUserName()} asked: \"{preview}\"");

            return new JsonResult(new { reply });
        }
        [IgnoreAntiforgeryToken]
        public IActionResult OnPostClearChat()
        {
            if (!_auth.IsStudent())
                return new JsonResult(new { success = false });

            var uid = _auth.GetUserID()!.Value;
            new CogniCareer.Data.AIChatData().ClearForUser(uid);
            return new JsonResult(new { success = true });
        }
        // ─────────────────────────────────────────────────────────
        //  NEW: AJAX handler — Explain why student matches a specific job
        // ─────────────────────────────────────────────────────────
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostExplainMatchAsync([FromBody] ExplainMatchRequest req)
        {
            if (!_auth.IsStudent())
                return new JsonResult(new { explanation = "Please sign in again." });

            var uid = _auth.GetUserID()!.Value;
            var skills = new CogniCareer.Data.StudentSkillData().GetByUserID(uid);
            var activeJobs = _jobService.GetAllActiveJobs();
            var ranked = _matchService.GetRankedJobs(uid, activeJobs);
            var jm = ranked.FirstOrDefault(j => j.Job.JobID == (req?.JobId ?? 0));

            if (jm == null)
                return new JsonResult(new { explanation = "Job not found or no longer active." });

            var gap = _matchService.GetGap(uid, jm.Job.JobID);
            string explanation = await _aiService.ExplainMatchAsync(jm.Job, jm.MatchScore, skills, gap);

            // Activity log
            new CogniCareer.Data.ActivityData().Log(
                "ai_explain",
                "Match Explanation Requested",
                $"{_auth.GetUserName()} asked about \"{jm.Job.Title}\" ({jm.MatchScore}% match)");

            return new JsonResult(new
            {
                jobTitle = jm.Job.Title,
                companyName = jm.Job.CompanyName,
                matchScore = jm.MatchScore,
                explanation
            });
        }
        // ─────────────────────────────────────────────────────────
        //  NEW: Extract text from an uploaded PDF resume
        //  Receives the file as multipart/form-data and returns the
        //  extracted text. JS then drops it into the textarea so
        //  the student can review/edit before clicking Analyze.
        // ─────────────────────────────────────────────────────────
        [IgnoreAntiforgeryToken]
        public IActionResult OnPostExtractPdf(IFormFile file)
        {
            if (!_auth.IsStudent())
                return new JsonResult(new { success = false, errorMessage = "Not signed in." });

            if (file == null || file.Length == 0)
                return new JsonResult(new { success = false, errorMessage = "No file received." });

            // Safety limits
            const long maxBytes = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxBytes)
                return new JsonResult(new { success = false, errorMessage = "File too large. Maximum 5 MB." });

            string ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                return new JsonResult(new { success = false, errorMessage = "Only PDF files are supported." });

            try
            {
                var sb = new StringBuilder();
                using (var stream = file.OpenReadStream())
                using (var pdf = PdfDocument.Open(stream))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                }

                string text = sb.ToString().Trim();
                if (text.Length < 20)
                    return new JsonResult(new { success = false, errorMessage = "PDF appears to be empty or image-based (scanned). Try a text-based PDF or paste the text manually." });

                // Activity log — separate from the AI call
                new CogniCareer.Data.ActivityData().Log(
                    "ai_resume_pdf",
                    "Resume PDF Uploaded",
                    $"{_auth.GetUserName()} uploaded '{file.FileName}' ({file.Length / 1024} KB, {text.Length} chars extracted)");

                return new JsonResult(new { success = true, text });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, errorMessage = "Could not read PDF: " + ex.Message });
            }
        }
        // ─────────────────────────────────────────────────────────
        //  NEW: AJAX handler — Generate a cover letter for a job
        // ─────────────────────────────────────────────────────────
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostGenerateCoverLetterAsync([FromBody] CoverLetterRequest req)
        {
            if (!_auth.IsStudent())
                return new JsonResult(new { success = false, errorMessage = "Please sign in again." });

            var uid = _auth.GetUserID()!.Value;
            var profile = _studentService.GetProfile(uid);
            var skills = new CogniCareer.Data.StudentSkillData().GetByUserID(uid);
            var activeJobs = _jobService.GetAllActiveJobs();
            var ranked = _matchService.GetRankedJobs(uid, activeJobs);
            var jm = ranked.FirstOrDefault(j => j.Job.JobID == (req?.JobId ?? 0));

            if (jm == null)
                return new JsonResult(new { success = false, errorMessage = "Job not found or no longer active." });

            var gap = _matchService.GetGap(uid, jm.Job.JobID);
            string tone = string.IsNullOrWhiteSpace(req?.Tone) ? "professional" : req!.Tone;

            string letter = await _aiService.GenerateCoverLetterAsync(jm.Job, profile, skills, gap, tone);

            // Activity log
            new CogniCareer.Data.ActivityData().Log(
                "ai_cover_letter",
                "Cover Letter Generated",
                $"{profile?.FullName ?? _auth.GetUserName()} generated a {tone} cover letter for \"{jm.Job.Title}\" at {jm.Job.CompanyName}");

            return new JsonResult(new
            {
                success = true,
                jobTitle = jm.Job.Title,
                companyName = jm.Job.CompanyName,
                letter
            });
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
        public class ExplainMatchRequest
        {
            public int JobId { get; set; }
        }
        public class CoverLetterRequest
        {
            public int JobId { get; set; }
            public string Tone { get; set; } = "professional";
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
            // === NEW: load chat history from DB so it persists across sessions ===
            ChatHistory = new CogniCareer.Data.AIChatData().GetForUser(uid, 50);
            ChatHistoryJson = JsonSerializer.Serialize(
                ChatHistory,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

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