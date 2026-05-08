using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;
using CogniCareer.Models;
using CogniCareer.Helpers;

namespace CogniCareer.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AuthService _auth;
        private readonly AdminService _adminService;
        private readonly CompanyService _companyService;

        public DashboardModel(AuthService auth, AdminService adminService, CompanyService companyService)
        {
            _auth = auth; _adminService = adminService; _companyService = companyService;
        }

        public string UserName { get; set; } = "";
        public AdminStats Stats { get; set; } = new();
        public List<global::CogniCareer.Models.Company> PendingCompanies { get; set; } = new();
        public List<global::CogniCareer.Models.Company> AllCompanies { get; set; } = new();
        public List<Skill> AllSkills { get; set; } = new();
        public List<User> AllUsers { get; set; } = new();
        public string SkillMessage { get; set; } = "";
        public bool SkillSuccess { get; set; }
        public string ToastMessage { get; set; } = "";
        public string ToastClass { get; set; } = "t-lime";

        public IActionResult OnGet()
        {
            if (!_auth.IsAdmin()) return RedirectToPage("/Auth/AdminAuth");
            LoadData();
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            _auth.Logout();
            return RedirectToPage("/Auth/AdminAuth");
        }

        public IActionResult OnPostApproveCompany(int companyId)
        {
            if (!_auth.IsAdmin()) return RedirectToPage("/Auth/AdminAuth");
            _companyService.Approve(companyId);
            TempData["Toast"] = "Company approved.";
            TempData["ToastClass"] = "t-lime";
            return RedirectToPage();
        }

        public IActionResult OnPostRejectCompany(int companyId)
        {
            if (!_auth.IsAdmin()) return RedirectToPage("/Auth/AdminAuth");
            _companyService.Reject(companyId);
            TempData["Toast"] = "Company rejected and removed.";
            TempData["ToastClass"] = "t-red";
            return RedirectToPage();
        }

        public IActionResult OnPostAddSkill(string SkillName, string Category)
        {
            if (!_auth.IsAdmin()) return RedirectToPage("/Auth/AdminAuth");
            var success = _adminService.AddSkill(SkillName.Trim(), Category);
            TempData["Toast"] = success ? $"Skill '{SkillName}' added!" : "Failed to add skill.";
            TempData["ToastClass"] = success ? "t-lime" : "t-red";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteSkill(int skillId)
        {
            if (!_auth.IsAdmin()) return RedirectToPage("/Auth/AdminAuth");
            _adminService.DeleteSkill(skillId);
            TempData["Toast"] = "Skill deactivated.";
            TempData["ToastClass"] = "t-red";
            return RedirectToPage();
        }

        public IActionResult OnPostToggleUser(int userId, bool isActive)
        {
            if (!_auth.IsAdmin()) return RedirectToPage("/Auth/AdminAuth");
            _adminService.ToggleUser(userId, isActive);
            TempData["Toast"] = isActive ? "User activated." : "User deactivated.";
            TempData["ToastClass"] = isActive ? "t-lime" : "t-red";
            return RedirectToPage();
        }

        private void LoadData()
        {
            UserName = _auth.GetUserName();
            Stats = _adminService.GetStats();
            PendingCompanies = _companyService.GetPending();
            AllCompanies = _companyService.GetAll();
            AllSkills = _adminService.GetAllSkills();
            AllUsers = _adminService.GetAllUsers();

            if (TempData.ContainsKey("Toast"))
            {
                ToastMessage = TempData["Toast"]?.ToString() ?? "";
                ToastClass = TempData["ToastClass"]?.ToString() ?? "t-lime";
            }
        }
    }
}