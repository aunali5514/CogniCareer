namespace CogniCareer.Models
{
    public class Job
    {
        public int JobID { get; set; }
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string JobType { get; set; } = "";
        public string Duration { get; set; } = "";
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime PostedAt { get; set; }
    }
}
