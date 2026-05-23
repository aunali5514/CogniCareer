namespace CogniCareer.Models
{
    public class PlatformActivityEvent
    {
        public string EventType { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime OccurredAt { get; set; }

        public string DotClass => EventType switch
        {
            "login" => "live-dot-register",
            "register" => "live-dot-register",
            "apply" => "live-dot-apply",
            "job" => "live-dot-job",
            "status" => "live-dot-status",
            "company" => "live-dot-company",
            "approved" => "live-dot-company",
            "score" => "live-dot-score",
            _ => "live-dot-register"
        };
    }
}
