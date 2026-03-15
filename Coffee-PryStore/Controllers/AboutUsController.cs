using Microsoft.AspNetCore.Mvc;


namespace Coffee_PryStore.Controllers
{
    public class AboutUsController : Controller
    {
        public IActionResult AboutUs()
        {
            var currentLanguage = Request.Cookies["lang"] ?? "en-US";
            ViewData["CurrentLanguage"] = currentLanguage;
            return View();
        }
    }
}
