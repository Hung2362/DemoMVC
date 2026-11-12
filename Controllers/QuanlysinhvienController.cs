using DemoMVC.Data;
using DemoMVC.Helpers;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers
{
    public class QuanlysinhvienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuanlysinhvienController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            string searchString = "",
            int pageIndex = 1,
            int pageSize = 5)
        {
            var query = _context.Quanlysinhviens
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(student =>
                    student.MaSV.Contains(searchString) ||
                    student.FullName.Contains(searchString) ||
                    student.Address.Contains(searchString) ||
                    student.University.Contains(searchString));
            }

            var pagedList = await PaginatedList<Quanlysinhvien>.CreateAsync(
                query.OrderBy(student => student.MaSV),
                pageIndex,
                pageSize);

            return Json(new
            {
                items = pagedList.Items,
                pageIndex = pagedList.PageIndex,
                totalPages = pagedList.TotalPages,
                totalCount = pagedList.TotalCount,
                hasPreviousPage = pagedList.HasPreviousPage,
                hasNextPage = pagedList.HasNextPage
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetById(string id)
        {
            var student = await _context.Quanlysinhviens
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.MaSV == id);

            return Json(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Quanlysinhvien sv)
        {
            if (sv is null)
            {
                return Json(new { success = false, message = "Dữ liệu sinh viên không hợp lệ." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = GetValidationErrors() });
            }

            var exists = await _context.Quanlysinhviens
                .AnyAsync(s => s.MaSV == sv.MaSV);

            if (exists)
            {
                return Json(new { success = false, message = "Mã sinh viên này đã tồn tại." });
            }

            _context.Quanlysinhviens.Add(sv);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thêm thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] Quanlysinhvien sv)
        {
            if (sv is null)
            {
                return Json(new { success = false, message = "Dữ liệu sinh viên không hợp lệ." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = GetValidationErrors() });
            }

            var student = await _context.Quanlysinhviens
                .FirstOrDefaultAsync(s => s.MaSV == sv.MaSV);

            if (student is null)
            {
                return Json(new { success = false, message = "Không tìm thấy sinh viên cần cập nhật." });
            }

            student.FullName = sv.FullName;
            student.Address = sv.Address;
            student.University = sv.University;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var student = await _context.Quanlysinhviens
                .FirstOrDefaultAsync(s => s.MaSV == id);

            if (student is null)
            {
                return Json(new { success = false, message = "Không tìm thấy sinh viên cần xóa." });
            }

            _context.Quanlysinhviens.Remove(student);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa thành công!" });
        }

        private string GetValidationErrors()
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message));

            var message = string.Join(" ", errors);
            return string.IsNullOrWhiteSpace(message)
                ? "Dữ liệu không hợp lệ."
                : message;
        }
    }
}
