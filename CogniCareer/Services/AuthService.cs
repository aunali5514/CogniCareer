using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class AuthService
    {
        private readonly UserData _userData = new();
        private readonly StudentData _studentData = new();
        private readonly CompanyData _companyData = new();
        private readonly ActivityData _activityData = new();
        private readonly IHttpContextAccessor _http;

        public AuthService(IHttpContextAccessor http)
        {
            _http = http;
        }

        public (bool success, string error) RegisterStudent(string fullName, string email, string password)
        {
            if (_userData.EmailExists(email)) return (false, "Email already registered.");
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Student"
            };
            int id = _userData.RegisterUser(user, null);
            return id > 0 ? (true, "") : (false, "Registration failed. Try again.");
        }

        public (bool success, string error, User? user) LoginStudent(string email, string password)
        {
            var user = _userData.GetByEmail(email);
            if (user == null || user.Role != "Student") return (false, "Account not found.", null);
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return (false, "Incorrect password.", null);
            if (!user.IsActive) return (false, "Account is deactivated.", null);
            SetSession(user);
            _activityData.TouchStudentLogin(user.UserID);
            return (true, "", user);
        }

        public (bool success, string error) RegisterCompany(string fullName, string email, string password,
            string companyName, string industry, string website, string description)
        {
            if (_userData.EmailExists(email)) return (false, "Email already registered.");
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Company"
            };
            int id = _userData.RegisterUser(user, null);
            if (id == 0) return (false, "Registration failed.");
            var company = new Company
            {
                UserID = id,
                CompanyName = companyName,
                Industry = industry,
                Website = website,
                Description = description
            };
            _companyData.InsertCompany(company);
            return (true, "");
        }

        public (bool success, string error, User? user) LoginCompany(string email, string password)
        {
            var user = _userData.GetByEmail(email);
            if (user == null || user.Role != "Company") return (false, "Account not found.", null);
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return (false, "Incorrect password.", null);
            if (!user.IsActive) return (false, "Account is deactivated.", null);
            var company = _companyData.GetByUserID(user.UserID);
            if (company == null) return (false, "Company profile not found.", null);
            if (!company.IsApproved) return (false, "Your company is pending admin approval.", null);
            SetSession(user);
            _activityData.Log("login", "Company signed in", $"{company.CompanyName} signed in to the company portal.");
            return (true, "", user);
        }

        public (bool success, string error, User? user) LoginAdmin(string email, string password)
        {
            var user = _userData.GetByEmail(email);

            // TEMP DEBUG
            if (user == null) return (false, "DEBUG: GetByEmail returned null", null);
            if (user.Role != "Admin") return (false, $"DEBUG: Role is '{user.Role}'", null);
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return (false, $"DEBUG: BCrypt failed. Hash='{user.PasswordHash}'", null);

            SetSession(user);
            _activityData.Log("login", "Admin signed in", $"{user.FullName} signed in to the admin panel.");
            return (true, "", user);
        }

        private void SetSession(User user)
        {
            var session = _http.HttpContext!.Session;
            session.SetInt32("UserID", user.UserID);
            session.SetString("UserName", user.FullName);
            session.SetString("UserEmail", user.Email);
            session.SetString("UserRole", user.Role);
        }

        public void Logout() => _http.HttpContext!.Session.Clear();

        public int? GetUserID() => _http.HttpContext!.Session.GetInt32("UserID");
        public string GetUserName() => _http.HttpContext!.Session.GetString("UserName") ?? "";
        public string GetUserRole() => _http.HttpContext!.Session.GetString("UserRole") ?? "";
        public bool IsLoggedIn() => GetUserID() != null;
        public bool IsStudent() => GetUserRole() == "Student";
        public bool IsCompany() => GetUserRole() == "Company";
        public bool IsAdmin() => GetUserRole() == "Admin";
    }
}