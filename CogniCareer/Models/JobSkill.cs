namespace CogniCareer.Models
{
    public class JobSkill
    {
        public int JobSkillID { get; set; }
        public int JobID { get; set; }
        public int SkillID { get; set; }
        public string SkillName { get; set; } = "";
        public string Priority { get; set; } = "";
    }
}