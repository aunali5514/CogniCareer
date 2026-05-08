using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class MatchScoreService
    {
        private readonly StudentSkillData _studentSkillData = new();
        private readonly JobSkillData _jobSkillData = new();

        public decimal Calculate(int userID, int jobID)
        {
            var studentSkills = _studentSkillData.GetByUserID(userID)
                .Select(s => s.SkillID).ToHashSet();
            var jobSkills = _jobSkillData.GetByJobID(jobID);
            if (!jobSkills.Any()) return 0;

            int required = jobSkills.Count(js => js.Priority == "Required");
            int preferred = jobSkills.Count(js => js.Priority == "Preferred");
            int matchedReq = jobSkills.Count(js => js.Priority == "Required" && studentSkills.Contains(js.SkillID));
            int matchedPref = jobSkills.Count(js => js.Priority == "Preferred" && studentSkills.Contains(js.SkillID));

            decimal reqScore = required > 0 ? (decimal)matchedReq / required * 70 : 70;
            decimal prefScore = preferred > 0 ? (decimal)matchedPref / preferred * 30 : 30;
            return Math.Round(reqScore + prefScore, 1);
        }

        public SkillGapResult GetGap(int userID, int jobID)
        {
            var studentSkillIDs = _studentSkillData.GetByUserID(userID)
                .Select(s => s.SkillID).ToHashSet();
            var jobSkills = _jobSkillData.GetByJobID(jobID);
            return new SkillGapResult
            {
                MatchedSkills = jobSkills.Where(js => studentSkillIDs.Contains(js.SkillID)).ToList(),
                MissingSkills = jobSkills.Where(js => !studentSkillIDs.Contains(js.SkillID)).ToList()
            };
        }

        public List<JobWithMatchScore> GetRankedJobs(int userID, List<Job> jobs)
        {
            return jobs.Select(j => new JobWithMatchScore
            {
                Job = j,
                MatchScore = Calculate(userID, j.JobID)
            })
            .OrderByDescending(x => x.MatchScore)
            .ToList();
        }
    }
}