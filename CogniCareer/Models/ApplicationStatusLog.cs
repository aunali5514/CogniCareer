namespace CogniCareer.Models
{
    public class ApplicationStatusLog
    {
        public int LogID { get; set; }
        public int ApplicationID { get; set; }
        public string Status { get; set; } = "";
        public DateTime ChangedAt { get; set; }
        public int ChangedByUserID { get; set; }
    }
}