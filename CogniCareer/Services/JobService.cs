using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class JobService
    {
        private readonly JobData _jobData = new();
        private readonly JobSkillData _jobSkillData = new();

        public List<Job> GetAllActiveJobs() => _jobData.GetAllActiveJobs();
        public List<Job> GetByCompany(int companyID) => _jobData.GetByCompany(companyID);
        public List<Job> GetAllJobs() => _jobData.GetAllJobs();
        public Job? GetById(int jobID) => _jobData.GetByID(jobID);
        public List<JobSkill> GetJobSkills(int jobID) => _jobSkillData.GetByJobID(jobID);

        public int PostJob(Job job, List<JobSkill> skills)
        {
            int jobID = _jobData.InsertJob(job);
            if (jobID > 0)
            {
                foreach (var s in skills)
                {
                    s.JobID = jobID;
                    _jobSkillData.AddJobSkill(s);
                }
            }
            return jobID;
        }

        public bool CloseJob(int jobID) => _jobData.CloseJob(jobID);
    }
}