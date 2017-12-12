using Sesame.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using Sesame.Web.Helpers;
using Sesame.Web.Services;

namespace Sesame.Web.Controllers
{
    public class HomeController : Controller
    {

        public HomeController()
        {
          
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Enrollment");
        }

       

        public IActionResult Error()
        {
            return View(new ErrorViewModel());
        }
    }
}
