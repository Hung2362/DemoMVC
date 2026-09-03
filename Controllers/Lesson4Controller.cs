using Microsoft.AspNetCore.Mvc;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    public class Lesson4Controller : Controller
    {
        // 1. GET: Hiển thị form ban đầu
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 2. POST: Nhận đối tượng Model sinh viên từ form
        [HttpPost]
        public IActionResult Index(Quanlysinhvien sv)
        {
            // Trả trực tiếp đối tượng Model sang View để hiển thị
            return View(sv);
        }
    }
}