using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;
using CogniCareer.Data;

namespace CogniCareer.Pages.Auth
{
    public class StudentAuthModel : PageModel
    {
        private readonly AuthService _auth;
        private readonly AdminService _adminService;

        public StudentAuthModel(AuthService auth, AdminService adminService)
        {
            _auth = auth;
            _adminService = adminService;
        }

        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";
        public bool ShowRegisterTab { get; set; } = false;
        public int TotalStudents { get; set; }
        public int TotalJobs { get; set; }

        public IActionResult OnGet()
        {
            if (_auth.IsStudent()) return RedirectToPage("/Student/Dashboard");
            var stats = _adminService.GetStats();
            TotalStudents = stats.TotalStudents;
            TotalJobs = stats.TotalActiveJobs;
            return Page();
        }

        public IActionResult OnPostLogin(string Email, string Password)
        {
            var stats = _adminService.GetStats();
            TotalStudents = stats.TotalStudents;
            TotalJobs = stats.TotalActiveJobs;

            var (success, error, user) = _auth.LoginStudent(Email, Password);
            if (!success) { ErrorMessage = error; return Page(); }
            return RedirectToPage("/Student/Dashboard");
        }

        public IActionResult OnPostRegister(string FullName, string Email, string Password, string ConfirmPassword)
        {
            var stats = _adminService.GetStats();
            TotalStudents = stats.TotalStudents;
            TotalJobs = stats.TotalActiveJobs;

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                ShowRegisterTab = true;
                return Page();
            }
            var (success, error) = _auth.RegisterStudent(FullName, Email, Password);
            if (!success) { ErrorMessage = error; ShowRegisterTab = true; return Page(); }
            SuccessMessage = "Account created! Please sign in.";
            return Page();
        }
    }
}