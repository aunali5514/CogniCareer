namespace CogniCareer.Models
{
    /// <summary>
    /// Result returned by AIService.AnalyzeResumeAsync.
    /// All score fields are integers from 0-100.
    /// </summary>
    public class ResumeAnalysisResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";

        public int OverallScore { get; set; }
        public int SkillsAlignment { get; set; }
        public int Completeness { get; set; }
        public int ExperienceLanguage { get; set; }
        public int KeywordDensity { get; set; }
        public string Feedback { get; set; } = "";

        public static ResumeAnalysisResult Error(string msg) =>
            new() { Success = false, ErrorMessage = msg };
    }
}