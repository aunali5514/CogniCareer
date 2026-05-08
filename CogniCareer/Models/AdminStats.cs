namespace CogniCareer.Models
{
    public class AdminStats
    {
        public int TotalStudents { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalActiveJobs { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApprovals { get; set; }
        public decimal AverageMatchScore { get; set; }
    }
}