using DemoMVC.Data;
using DemoMVC.Helpers;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers
{
    public class QuanlymonhocController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuanlymonhocController(ApplicationDbContext context)
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
            var query = _context.Quanlymonhocs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(subject =>
                    subject.MaMH.Contains(searchString) ||
                    subject.TenMH.Contains(searchString) ||
                    subject.KhoaPhuTrach.Contains(searchString));
            }

            var pagedList = await PaginatedList<Quanlymonhoc>.CreateAsync(
                query.OrderBy(subject => subject.MaMH),
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
            var subject = await _context.Quanlymonhocs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.MaMH == id);

            return Json(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] Quanlymonhoc subject)
        {
            if (subject is null)
            {
                return Json(new { success = false, message = "Dữ liệu môn học không hợp lệ." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = GetValidationErrors() });
            }

            var exists = await _context.Quanlymonhocs
                .AnyAsync(item => item.MaMH == subject.MaMH);

            if (exists)
            {
                return Json(new { success = false, message = "Mã môn học này đã tồn tại." });
            }

            _context.Quanlymonhocs.Add(subject);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thêm môn học thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] Quanlymonhoc subject)
        {
            if (subject is null)
            {
                return Json(new { success = false, message = "Dữ liệu môn học không hợp lệ." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = GetValidationErrors() });
            }

            var existingSubject = await _context.Quanlymonhocs
                .FirstOrDefaultAsync(item => item.MaMH == subject.MaMH);

            if (existingSubject is null)
            {
                return Json(new { success = false, message = "Không tìm thấy môn học cần cập nhật." });
            }

            existingSubject.TenMH = subject.TenMH;
            existingSubject.SoTinChi = subject.SoTinChi;
            existingSubject.KhoaPhuTrach = subject.KhoaPhuTrach;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật môn học thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var subject = await _context.Quanlymonhocs
                .FirstOrDefaultAsync(item => item.MaMH == id);

            if (subject is null)
            {
                return Json(new { success = false, message = "Không tìm thấy môn học cần xóa." });
            }

            _context.Quanlymonhocs.Remove(subject);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa môn học thành công!" });
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
