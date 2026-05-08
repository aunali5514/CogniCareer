using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class AdminService
    {
        private readonly AdminData _adminData = new();
        private readonly SkillData _skillData = new();
        private readonly UserData _userData = new();

        public AdminStats GetStats() => _adminData.GetStats();
        public List<User> GetAllUsers() => _adminData.GetAllUsers();
        public List<Skill> GetAllSkills() => _skillData.GetAllSkills();
        public List<Skill> GetActiveSkills() => _skillData.GetActiveSkills();
        public bool AddSkill(string name, string category) =>
            _skillData.AddSkill(new Skill { SkillName = name, Category = category });
        public bool DeleteSkill(int skillID) => _skillData.DeleteSkill(skillID);
        public bool ToggleUser(int userID, bool isActive) => _userData.UpdateStatus(userID, isActive);
    }
}