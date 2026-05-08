namespace CogniCareer.Models
{
    public class ApplicationNote
    {
        public int NoteID { get; set; }
        public int ApplicationID { get; set; }
        public string NoteText { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}