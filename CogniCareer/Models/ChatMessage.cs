namespace CogniCareer.Models
{
    /// <summary>
    /// One turn of conversation between the student and the AI advisor.
    /// Role is either "user" (the student) or "model" (the AI).
    /// History is kept client-side and sent back with each request.
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }
}