namespace CogniCareer.Models
{
    public class Application
    {
        public int ApplicationID { get; set; }
        public int JobID { get; set; }
        public int UserID { get; set; }
        public decimal MatchScore { get; set; }
        public DateTime AppliedAt { get; set; }
        public string CurrentStatus { get; set; } = "Applied";
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
    }
}