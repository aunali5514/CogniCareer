using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Models;
using CogniCareer.Services;

namespace CogniCareer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuthService _auth;
        private readonly AdminService _admin;

        public IndexModel(AuthService auth, AdminService admin)
        {
            _auth = auth;
            _admin = admin;
        }

        public HomePublicStats Stats { get; set; } = new();

        public IActionResult OnGet()
        {
            if (_auth.IsLoggedIn())
            {
                return _auth.GetUserRole() switch
                {
                    "Student" => RedirectToPage("/Student/Dashboard"),
                    "Company" => RedirectToPage("/Company/Dashboard"),
                    "Admin" => RedirectToPage("/Admin/Dashboard"),
                    _ => Page()
                };
            }

            Stats = _admin.GetPublicHomeStats() ?? new HomePublicStats();
            return Page();
        }
    }
}
