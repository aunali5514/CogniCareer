using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;

namespace CogniCareer.Pages.Auth
{
    public class AdminAuthModel : PageModel
    {
        private readonly AuthService _auth;
        public AdminAuthModel(AuthService auth) { _auth = auth; }

        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet()
        {
            if (_auth.IsAdmin()) return RedirectToPage("/Admin/Dashboard");
            return Page();
        }

        public IActionResult OnPost(string Email, string Password)
        {
            var (success, error, user) = _auth.LoginAdmin(Email, Password);
            if (!success) { ErrorMessage = error; return Page(); }
            return RedirectToPage("/Admin/Dashboard");
        }
    }
}