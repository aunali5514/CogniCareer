namespace CogniCareer.Helpers
{
    public static class SessionHelper
    {
        public static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }

        public static string GetMatchColor(decimal score) => score switch
        {
            >= 80 => "#1a7a3a",
            >= 60 => "#b46400",
            _ => "var(--red)"
        };

        public static string GetMatchClass(decimal score) => score switch
        {
            >= 80 => "ring-fill-high",
            >= 60 => "ring-fill-mid",
            _ => "ring-fill-low"
        };

        public static decimal GetRingOffset(decimal score)
        {
            // SVG ring: circumference = 145
            return Math.Round(145 - (score / 100m * 145), 1);
        }

        public static string GetStatusClass(string status) => status switch
        {
            "Applied" => "st-pending",
            "Shortlisted" => "st-review",
            "Hired" => "st-active",
            "Rejected" => "st-reject",
            _ => "st-pending"
        };
    }
}