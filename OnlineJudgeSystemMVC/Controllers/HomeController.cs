using Microsoft.AspNetCore.Mvc;

namespace OnlineJudgeSystemMVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult ChallengeMeInfo()
        {
            return View();
        }
    }
}