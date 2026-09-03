using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Controllers
{
    public class QuanlysinhvienController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string fullName, string address, string university)
        {
            ViewBag.Message = $"FullName: {fullName}, Address: {address}, University: {university?.ToUpper()}";
            return View();
        }
    }
}