namespace CogniCareer.Models
{
    public class LearningResource
    {
        public int ResourceID { get; set; }
        public int SkillID { get; set; }
        public string Title { get; set; } = "";
        public string Provider { get; set; } = "";
        public string URL { get; set; } = "";
        public string ResourceType { get; set; } = "";
        public bool IsFree { get; set; }
    }
}