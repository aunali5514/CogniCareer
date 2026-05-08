namespace CogniCareer.Models
{
    public class StudentSkill
    {
        public int StudentSkillID { get; set; }
        public int UserID { get; set; }
        public int SkillID { get; set; }
        public string SkillName { get; set; } = "";
        public string Category { get; set; } = "";
        public string ProficiencyLevel { get; set; } = "";
    }
}