using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CogniCareer.Services;

namespace CogniCareer.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuthService _auth;
        public IndexModel(AuthService auth) { _auth = auth; }

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
            return Page();
        }
    }
}