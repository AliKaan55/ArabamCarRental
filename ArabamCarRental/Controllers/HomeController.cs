using ArabamCarRental.Services;
using System.Linq;
using System.Web.Mvc;

namespace ArabamCarRental.Controllers
{
    public class HomeController : Controller
    {
        private readonly CarService _carService = new CarService();

        public ActionResult Index()
        {
            var featured = _carService.GetAllCars()
                                      .OrderBy(c => !c.IsAvailable)
                                      .Take(6)
                                      .ToList();
            ViewBag.FeaturedCars = featured;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}
