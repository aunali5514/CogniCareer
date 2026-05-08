namespace CogniCareer.Models
{
    public class SkillGapResult
    {
        public List<JobSkill> MatchedSkills { get; set; } = new();
        public List<JobSkill> MissingSkills { get; set; } = new();
    }
}
