using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class StudentService
    {
        private readonly StudentData _studentData = new();
        private readonly UserData _userData = new();

        public StudentProfile? GetProfile(int userID)
        {
            var profile = _studentData.GetProfile(userID);
            if (profile != null)
            {
                var user = _userData.GetByID(userID);
                if (user != null)
                {
                    profile.FullName = user.FullName;
                    profile.Email = user.Email;
                }
            }
            return profile;
        }

        public bool SaveProfile(StudentProfile p)
        {
            if (_studentData.ProfileExists(p.UserID))
                return _studentData.UpdateProfile(p);
            return _studentData.InsertProfile(p);
        }

        public bool ProfileExists(int userID) => _studentData.ProfileExists(userID);
    }
}