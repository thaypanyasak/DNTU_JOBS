using DNTU_JOBS.Models;

namespace DNTU_JOBS.Helper
{
    public static class StatusHelper
    {
        public static string GetStatusCssClass(ApprovalStatus status)
        {
            return status switch
            {
                ApprovalStatus.Pending => "text-warning",
                ApprovalStatus.Approved => "text-success",
                ApprovalStatus.Rejected => "text-danger",
                _ => "text-muted"
            };
        }
    }
}
