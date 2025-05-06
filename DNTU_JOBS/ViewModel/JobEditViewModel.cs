using DNTU_JOBS.Models;

namespace DNTU_JOBS.ViewModel
{
    public class JobEditViewModel
    {
        public JobTable EditedJob { get; set; }
        public JobTable OriginalJob { get; set; }
    }
}
