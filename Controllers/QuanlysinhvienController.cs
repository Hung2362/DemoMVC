using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    public class QuanlysinhvienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuanlysinhvienController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ: Danh sách sinh viên
        public async Task<IActionResult> Index()
        {
            var list = await _context.Quanlysinhviens.ToListAsync();
            return View(list);
        }

        // 2. CREATE: Hiển thị form thêm mới
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // CREATE (POST): Xử lý thêm mới kèm kiểm tra Validate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quanlysinhvien sv)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Quanlysinhviens.AnyAsync(s => s.MaSV == sv.MaSV))
                {
                    ModelState.AddModelError("MaSV", "Mã sinh viên này đã tồn tại.");
                    return View(sv);
                }

                _context.Add(sv);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Thêm mới sinh viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(sv);
        }

        // 3. UPDATE: Hiển thị form cập nhật
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var sv = await _context.Quanlysinhviens.FindAsync(id);
            if (sv == null) return NotFound();

            return View(sv);
        }

        // UPDATE (POST): Lưu thông tin chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Quanlysinhvien sv)
        {
            if (id != sv.MaSV) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sv);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Cập nhật sinh viên thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Quanlysinhviens.Any(s => s.MaSV == sv.MaSV))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(sv);
        }

        // 4. DELETE: Xác nhận xóa
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var sv = await _context.Quanlysinhviens.FirstOrDefaultAsync(m => m.MaSV == id);
            if (sv == null) return NotFound();

            return View(sv);
        }

        // DELETE (POST): Thực hiện xóa khỏi CSDL
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var sv = await _context.Quanlysinhviens.FindAsync(id);
            if (sv != null)
            {
                _context.Quanlysinhviens.Remove(sv);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Xóa sinh viên thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}