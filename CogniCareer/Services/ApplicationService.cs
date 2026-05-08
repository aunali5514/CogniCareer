using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class ApplicationService
    {
        private readonly ApplicationData _appData = new();
        private readonly StatusLogData _logData = new();
        private readonly NoteData _noteData = new();
        private readonly AlertData _alertData = new();

        public List<Application> GetByUser(int userID) => _appData.GetByUser(userID);
        public List<Application> GetByJob(int jobID) => _appData.GetByJob(jobID);
        public bool AlreadyApplied(int userID, int jobID) => _appData.ApplicationExists(userID, jobID);
        public PeerBenchmark GetBenchmark(int userID, int jobID) => _appData.GetPeerBenchmark(userID, jobID);

        public (bool success, string msg) Apply(int userID, int jobID, decimal matchScore)
        {
            if (_appData.ApplicationExists(userID, jobID)) return (false, "Already applied.");
            var app = new Application { UserID = userID, JobID = jobID, MatchScore = matchScore };
            int appID = _appData.InsertApplication(app);
            if (appID == 0) return (false, "Application failed.");
            _logData.InsertLog(new ApplicationStatusLog
            {
                ApplicationID = appID,
                Status = "Applied",
                ChangedByUserID = userID
            });
            _alertData.InsertAlert(new Alert
            {
                UserID = userID,
                Message = "Your application was submitted successfully.",
                AlertType = "Application"
            });
            return (true, "Application submitted!");
        }

        public bool UpdateStatus(int applicationID, string status, int changedByUserID)
        {
            _logData.InsertLog(new ApplicationStatusLog
            {
                ApplicationID = applicationID,
                Status = status,
                ChangedByUserID = changedByUserID
            });
            return true;
        }

        public List<ApplicationStatusLog> GetHistory(int applicationID) =>
            _logData.GetByApplication(applicationID);

        public bool AddNote(int applicationID, string noteText) =>
            _noteData.AddNote(new ApplicationNote { ApplicationID = applicationID, NoteText = noteText });

        public List<ApplicationNote> GetNotes(int applicationID) =>
            _noteData.GetByApplication(applicationID);
    }
}