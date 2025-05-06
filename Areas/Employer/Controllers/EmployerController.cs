using Microsoft.AspNetCore.Mvc;

namespace DNTU_JOBS.Areas.Employer.Controllers
{
    [Area("Employer")]
    public class EmployerController : Controller
    {
        public IActionResult LoadNotifications(int page = 1)
        {
            return ViewComponent("Notification", new { page = page });
        }
    }
}
