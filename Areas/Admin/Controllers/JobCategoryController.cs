using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DNTU_JOBS.Models;
using DNTU_JOBS.Data;
using DNTU_JOBS.ViewModel;

namespace DNTU_JOBS.Controllers
{
    [Area("Admin")]
    public class JobCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: JobCategory
        public IActionResult Index(int page = 1, int pageSize = 10)
        {
            // Tổng số danh mục công việc
            var totalCategories = _context.JobCategories.Count();

            if (totalCategories == 0)
            {
                ViewBag.TotalPages = 0;
                ViewBag.CurrentPage = 0;
                return View(new List<JobCategoryTable>()); 
            }

            // Tính tổng số trang
            int totalPages = (int)Math.Ceiling(totalCategories / (double)pageSize);

            // Đảm bảo giá trị `page` nằm trong phạm vi hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            // Lấy danh mục công việc với phân trang
            var categories = _context.JobCategories
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền thông tin phân trang vào ViewBag
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(categories);
        }



        // GET: JobCategory/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: JobCategory/Create
        [HttpPost]
        public IActionResult Create(JobCategoryTable jobCategory)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.JobCategories.Add(jobCategory);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while saving: {ex.Message}");
                    ModelState.AddModelError("", "An unexpected error occurred while saving the category.");
                }
            }
            else
            {
                Console.WriteLine("ModelState is invalid:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($" - {error.ErrorMessage}");
                }
            }

            // Nếu có lỗi, trả lại view để người dùng chỉnh sửa
            return View(jobCategory);
        }



        public IActionResult Edit(int id)
        {
            var category = _context.JobCategories.Find(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: JobCategory/Edit/{id}
        [HttpPost]

        public IActionResult Edit(int id, JobCategoryTable jobCategory)
        {
            if (id != jobCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(jobCategory);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(jobCategory);
        }

        public IActionResult Delete(int id)
        {
            var category = _context.JobCategories.Find(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        public IActionResult DeleteAjax(int id)
        {
            var category = _context.JobCategories.Find(id);
            if (category != null)
            {
                _context.JobCategories.Remove(category);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }


    }
}
