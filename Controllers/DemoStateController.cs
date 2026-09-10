using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Controllers
{
    public class DemoStateController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // 1. ViewData: Dùng để gán tiêu đề trang
            ViewData["TitlePage"] = "Ví dụ Quản lý trạng thái";

            return View();
        }

        [HttpPost]
        public IActionResult Index(string studentName, string className)
        {
            // 2. ViewBag: Hiển thị dữ liệu trả về ngay trên trang hiện tại
            ViewBag.Info = $"Sinh viên: {studentName} - Lớp: {className}";

            ViewData["TitlePage"] = "Ví dụ Quản lý trạng thái";
            return View();
        }

        [HttpPost]
        public IActionResult SaveData(string studentName)
        {
            // 3. TempData: Giữ lại dữ liệu khi chuyển hướng (Redirect)
            TempData["AlertMessage"] = $"Lưu thành công hồ sơ của {studentName}!";

            return RedirectToAction("Index");
        }
    }
}