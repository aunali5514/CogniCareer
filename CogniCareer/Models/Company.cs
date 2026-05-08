namespace CogniCareer.Models
{
    public class Company
    {
        public int CompanyID { get; set; }
        public int UserID { get; set; }
        public string CompanyName { get; set; } = "";
        public string Industry { get; set; } = "";
        public string Website { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
