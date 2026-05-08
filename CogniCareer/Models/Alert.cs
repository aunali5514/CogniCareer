namespace CogniCareer.Models
{
    public class Alert
    {
        public int AlertID { get; set; }
        public int UserID { get; set; }
        public string Message { get; set; } = "";
        public string AlertType { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
