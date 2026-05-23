using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class AdminService
    {
        private readonly AdminData _adminData = new();
        private readonly SkillData _skillData = new();
        private readonly UserData _userData = new();
        private readonly ActivityData _activityData = new();

        public AdminStats GetStats()
        {
            var stats = _adminData.GetStats() ?? new AdminStats();
            _adminData.EnrichDashboardStats(stats);
            return stats;
        }
        public HomePublicStats GetPublicHomeStats() => _adminData.GetPublicHomeStats();
        public List<User> GetAllUsers() => _adminData.GetAllUsers();
        public List<Skill> GetAllSkills() => _skillData.GetAllSkills();
        public List<Skill> GetActiveSkills() => _skillData.GetActiveSkills();
        public bool AddSkill(string name, string category) =>
            _skillData.AddSkill(new Skill { SkillName = name, Category = category });
        public bool DeleteSkill(int skillID) => _skillData.DeleteSkill(skillID);
        public bool ToggleUser(int userID, bool isActive) => _userData.UpdateStatus(userID, isActive);
        public List<SkillInsight> GetTopDemandingSkills(int top = 5) => _adminData.GetTopDemandingSkills(top);
        public List<SkillInsight> GetMostMissingSkills(int top = 5) => _adminData.GetMostMissingSkills(top);
        public List<CompanyInsight> GetTopCompanies(int top = 5) => _adminData.GetTopCompanies(top);
        public List<AdminStudentRow> GetAllStudents() => _adminData.GetAllStudents();
        public bool ToggleStudent(int studentId, bool isActive) => _adminData.ToggleStudentStatus(studentId, isActive);
        public List<PlatformActivityEvent> GetRecentActivity(DateTime? sinceUtc, int limit = 30) =>
            _activityData.GetRecent(sinceUtc, limit);
        public void LogActivity(string eventType, string title, string description) =>
            _activityData.Log(eventType, title, description);
    }
}