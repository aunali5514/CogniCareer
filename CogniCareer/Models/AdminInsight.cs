namespace CogniCareer.Models
{
    public class SkillInsight
    {
        public string SkillName { get; set; } = "";
        public string Category { get; set; } = "";
        public int JobCount { get; set; }
        public int StudentCount { get; set; }
        public int DemandPct { get; set; }
    }

    public class CompanyInsight
    {
        public string CompanyName { get; set; } = "";
        public int JobCount { get; set; }
        public int ApplicationCount { get; set; }
    }
}
