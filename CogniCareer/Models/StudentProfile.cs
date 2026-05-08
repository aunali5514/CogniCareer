namespace CogniCareer.Models
{
    public class StudentProfile
    {
        public int ProfileID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string University { get; set; } = "";
        public string Degree { get; set; } = "";
        public int Semester { get; set; }
        public decimal GPA { get; set; }
        public int ExpectedGradYear { get; set; }
        public bool IsProfileComplete { get; set; }
    }
}