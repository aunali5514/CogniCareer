namespace CogniCareer.Models
{
    public class LearningResource
    {
        public int ResourceID { get; set; }
        public int SkillID { get; set; }
        public string Title { get; set; } = "";
        public string URL { get; set; } = "";
        public string Platform { get; set; } = "";
    }
}