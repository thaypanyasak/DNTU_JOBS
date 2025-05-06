namespace DNTU_JOBS.Helper
{
    public static class DateTimeExtensions
    {
        public static string ToRelativeTime(this DateTime dateTime)
        {
            var timeSpan = DateTime.Now.Subtract(dateTime);

            if (timeSpan.TotalMinutes < 1)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes > 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)timeSpan.TotalDays / 30} month{((int)timeSpan.TotalDays / 30 > 1 ? "s" : "")} ago";

            return $"{(int)timeSpan.TotalDays / 365} year{((int)timeSpan.TotalDays / 365 > 1 ? "s" : "")} ago";
        }
    }
}
