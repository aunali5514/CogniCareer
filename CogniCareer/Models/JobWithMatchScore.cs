namespace CogniCareer.Models
{
    public class JobWithMatchScore
    {
        public Job Job { get; set; } = new();
        public decimal MatchScore { get; set; }
    }
}
