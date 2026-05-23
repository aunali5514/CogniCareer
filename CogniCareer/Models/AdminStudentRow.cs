namespace CogniCareer.Models
{
    public class AdminStudentRow
    {
        public int StudentId { get; set; }
        public int? UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string University { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime? JoinedAt { get; set; }
    }
}
