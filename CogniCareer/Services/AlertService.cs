using CogniCareer.Data;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    public class AlertService
    {
        private readonly AlertData _alertData = new();

        public List<Alert> GetUnread(int userID) => _alertData.GetUnread(userID);
        public bool MarkRead(int alertID) => _alertData.MarkRead(alertID);
        public bool MarkAllRead(int userID) => _alertData.MarkAllRead(userID);
        public bool Send(int userID, string message, string alertType) =>
            _alertData.InsertAlert(new Alert { UserID = userID, Message = message, AlertType = alertType });
    }
}