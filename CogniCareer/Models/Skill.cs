namespace CogniCareer.Models
{
    public class Skill
    {
        public int SkillID { get; set; }
        public string SkillName { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}