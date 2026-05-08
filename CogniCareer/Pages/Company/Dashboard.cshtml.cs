using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;
using CogniCareer.Models;
using CogniCareer.Helpers;
using System.Text.Json;

namespace CogniCareer.Pages.Company
{
    public class DashboardModel : PageModel
    {
        private readonly AuthService _auth;
        private readonly CompanyService _companyService;
        private readonly JobService _jobService;
        private readonly ApplicationService _appService;
        private readonly AdminService _adminService;

        public DashboardModel(AuthService auth, CompanyService companyService, JobService jobService,
            ApplicationService appService, AdminService adminService)
        {
            _auth = auth; _companyService = companyService; _jobService = jobService;
            _appService = appService; _adminService = adminService;
        }

        public string UserName { get; set; } = "";
        public string Initials { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public global::CogniCareer.Models.Company? Company { get; set; }
        public List<global::CogniCareer.Models.Job> Jobs { get; set; } = new();
        public Dictionary<int, List<global::CogniCareer.Models.Application>> ApplicationsByJob { get; set; } = new();
        public string AllSkillsJson { get; set; } = "[]";
        public string AllApplicationsJson { get; set; } = "[]";
        public string PostMessage { get; set; } = "";
        public bool PostSuccess { get; set; }
        public string ProfileMessage { get; set; } = "";
        public string ToastMessage { get; set; } = "";
        public string ToastClass { get; set; } = "t-lime";
        public int TotalJobs => Jobs.Count(j => j.Status == "Active");
        public int TotalApplications => ApplicationsByJob.Values.Sum(a => a.Count);
        public int ShortlistedCount => ApplicationsByJob.Values.SelectMany(a => a).Count(a => a.CurrentStatus == "Shortlisted");
        public decimal AvgMatchScore => ApplicationsByJob.Values.SelectMany(a => a).Any()
            ? Math.Round(ApplicationsByJob.Values.SelectMany(a => a).Average(a => a.MatchScore), 1) : 0;

        public List<Application> GetApplicationsByJob(int jobId) =>
            ApplicationsByJob.ContainsKey(jobId) ? ApplicationsByJob[jobId] : new();

        public IActionResult OnGet()
        {
            if (!_auth.IsCompany()) return RedirectToPage("/Auth/CompanyAuth");
            LoadData();
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            _auth.Logout();
            return RedirectToPage("/Auth/CompanyAuth");
        }

        public IActionResult OnPostPostJob(string Title, string Description, string JobType, string Duration,
            DateTime Deadline, string SkillsJson)
        {
            if (!_auth.IsCompany()) return RedirectToPage("/Auth/CompanyAuth");
            var uid = _auth.GetUserID()!.Value;
            var company = _companyService.GetByUserID(uid);
            if (company == null) { TempData["Toast"] = "Company profile not found."; TempData["ToastClass"] = "t-red"; return RedirectToPage(); }

            var job = new Job { CompanyID = company.CompanyID, Title = Title, Description = Description, JobType = JobType, Duration = Duration, Deadline = Deadline };
            var skills = new List<JobSkill>();
            try
            {
                var parsed = JsonSerializer.Deserialize<List<JsonElement>>(SkillsJson ?? "[]");
                if (parsed != null)
                {
                    foreach (var el in parsed)
                    {
                        skills.Add(new JobSkill
                        {
                            SkillID = el.GetProperty("skillId").GetInt32(),
                            Priority = el.GetProperty("priority").GetString() ?? "Required"
                        });
                    }
                }
            }
            catch { }

            var jobId = _jobService.PostJob(job, skills);
            TempData["Toast"] = jobId > 0 ? "Job posted successfully! Matching is live." : "Failed to post job.";
            TempData["ToastClass"] = jobId > 0 ? "t-lime" : "t-red";
            return RedirectToPage();
        }

        public IActionResult OnPostCloseJob(int jobId)
        {
            if (!_auth.IsCompany()) return RedirectToPage("/Auth/CompanyAuth");
            _jobService.CloseJob(jobId);
            TempData["Toast"] = "Job closed.";
            TempData["ToastClass"] = "t-red";
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateStatus(int applicationId, string status)
        {
            if (!_auth.IsCompany()) return RedirectToPage("/Auth/CompanyAuth");
            _appService.UpdateStatus(applicationId, status, _auth.GetUserID()!.Value);
            TempData["Toast"] = "Status updated to " + status + ".";
            TempData["ToastClass"] = "t-lime";
            return RedirectToPage();
        }

        public IActionResult OnPostSaveProfile(string CompanyName, string Industry, string Website, string Description)
        {
            if (!_auth.IsCompany()) return RedirectToPage("/Auth/CompanyAuth");
            var uid = _auth.GetUserID()!.Value;
            var company = _companyService.GetByUserID(uid);
            if (company != null)
            {
                company.CompanyName = CompanyName;
                company.Industry = Industry;
                company.Website = Website;
                company.Description = Description;
                _companyService.Update(company);
                TempData["Toast"] = "Profile saved!";
                TempData["ToastClass"] = "t-lime";
            }
            return RedirectToPage();
        }

        private void LoadData()
        {
            var uid = _auth.GetUserID()!.Value;
            UserName = _auth.GetUserName();
            Initials = SessionHelper.GetInitials(UserName);
            Company = _companyService.GetByUserID(uid);
            CompanyName = Company?.CompanyName ?? UserName;

            if (Company != null)
            {
                Jobs = _jobService.GetByCompany(Company.CompanyID);
                foreach (var job in Jobs)
                    ApplicationsByJob[job.JobID] = _appService.GetByJob(job.JobID);
            }

            var allSkillsList = _adminService.GetActiveSkills()
                .Select(s => new { skillID = s.SkillID, skillName = s.SkillName, category = s.Category }).ToList();
            AllSkillsJson = JsonSerializer.Serialize(allSkillsList);

            var allApps = ApplicationsByJob.Values.SelectMany(a => a).Select(a => new
            {
                applicationID = a.ApplicationID,
                jobID = a.JobID,
                matchScore = a.MatchScore,
                appliedAt = a.AppliedAt.ToString("MMM dd"),
                currentStatus = a.CurrentStatus,
                studentName = "Applicant #" + a.ApplicationID
            }).ToList();
            AllApplicationsJson = JsonSerializer.Serialize(allApps);

            if (TempData.ContainsKey("Toast"))
            {
                ToastMessage = TempData["Toast"]?.ToString() ?? "";
                ToastClass = TempData["ToastClass"]?.ToString() ?? "t-lime";
            }
        }
    }
}