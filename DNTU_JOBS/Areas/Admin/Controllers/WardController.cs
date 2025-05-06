//using DNTU_JOBS.Data;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace DNTU_JOBS.Areas.Admin.Controllers
//{
//    [Area("Employer")]
//    [Route("Employer/[controller]")]
//    public class WardsController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;

//        public WardsController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        [HttpGet("bydistrict/{districtId}")]
//        public async Task<IActionResult> GetWardsByDistrict(int districtId)
//        {
//            var wards = await _context.Wards
//                .Where(w => w.DistrictId == districtId)
//                .Select(w => new { w.WardId, w.Name })
//                .ToListAsync();

//            return Ok(wards);
//        }

//    }
//}
