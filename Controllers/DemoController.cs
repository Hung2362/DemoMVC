using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Controllers
{
    public class DemoController : Controller
    {
        // GET: /Demo/ hoặc /Demo/Index
        public string Index()
        {
            return "This is my default action...";
        }
    }
}