using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;

namespace CogniCareer.Pages.Auth
{
    public class CompanyAuthModel : PageModel
    {
        private readonly AuthService _auth;
        public CompanyAuthModel(AuthService auth) { _auth = auth; }

        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";
        public bool ShowRegisterTab { get; set; } = false;

        public IActionResult OnGet()
        {
            if (_auth.IsCompany()) return RedirectToPage("/Company/Dashboard");
            return Page();
        }

        public IActionResult OnPostLogin(string Email, string Password)
        {
            var (success, error, user) = _auth.LoginCompany(Email, Password);
            if (!success) { ErrorMessage = error; return Page(); }
            return RedirectToPage("/Company/Dashboard");
        }

        public IActionResult OnPostRegister(string FullName, string Email, string Password,
            string CompanyName, string Industry, string Website, string Description)
        {
            var (success, error) = _auth.RegisterCompany(FullName, Email, Password, CompanyName, Industry, Website, Description);
            if (!success) { ErrorMessage = error; ShowRegisterTab = true; return Page(); }
            SuccessMessage = "Registration submitted! Wait for admin approval before logging in.";
            return Page();
        }
    }
}
