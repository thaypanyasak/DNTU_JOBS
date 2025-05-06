using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DNTU_JOBS.Authorization
{
    public class CanManageJobHandler : AuthorizationHandler<CanManageJobRequirement>
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CanManageJobHandler(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanManageJobRequirement requirement)
        {
            var userId = _userManager.GetUserId(context.User);
            if (userId == null)
            {
                context.Fail();
                return;
            }

            var admin = await _db.Admins.SingleOrDefaultAsync(a => a.ApplicationUserId == userId);
            if (admin != null && admin.CanManageJobs)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}
