using ArabamCarRental.Services;
using System;
using System.Web;
using System.Web.Mvc;

namespace ArabamCarRental.Controllers
{
    public class CarController : Controller
    {
        private readonly CarService _carService;

        public CarController()
        {
            _carService = new CarService();
        }

        public ActionResult Index(string searchBrand, decimal? minPrice, decimal? maxPrice, string vites, string yakit)
        {
            ViewBag.CurrentBrand    = searchBrand;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentVites    = vites;
            ViewBag.CurrentYakit    = yakit;
            ViewBag.Brands          = _carService.GetDistinctBrands();

            var cars = _carService.GetAllCars(searchBrand, minPrice, maxPrice, vites, yakit);
            return View(cars);
        }

        public ActionResult Details(int id)
        {
            var car = _carService.GetCarById(id);
            if (car == null) return HttpNotFound();
            ViewBag.Reviews = _carService.GetReviewsByCarId(id);
            ViewBag.IsLoggedIn = Session["UserId"] != null;
            return View(car);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(int CarId, int Rating, string Comment, HttpPostedFileBase ReviewPhoto)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Yorum yapabilmek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            int customerId = Convert.ToInt32(Session["UserId"]);

            if (string.IsNullOrWhiteSpace(Comment) || Comment.Length > 500)
            {
                TempData["ErrorMessage"] = "Yorum metni 1-500 karakter arasında olmalıdır.";
                return RedirectToAction("Details", new { id = CarId });
            }

            if (Rating < 1 || Rating > 5) Rating = 3;

            // Fotoğraf kaydet
            string photoUrl = null;
            if (ReviewPhoto != null && ReviewPhoto.ContentLength > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = System.IO.Path.GetExtension(ReviewPhoto.FileName).ToLower();
                if (System.Array.IndexOf(allowed, ext) >= 0)
                {
                    var folder = Server.MapPath("~/Content/ReviewPhotos/");
                    if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                    var fileName = System.Guid.NewGuid().ToString("N") + ext;
                    ReviewPhoto.SaveAs(System.IO.Path.Combine(folder, fileName));
                    photoUrl = "/Content/ReviewPhotos/" + fileName;
                }
            }

            bool success = _carService.AddReview(CarId, customerId, Rating, Comment.Trim(), photoUrl);

            if (success)
                TempData["ReviewSuccess"] = "Yorumunuz başarıyla eklendi!";
            else
                TempData["ErrorMessage"] = "Yorum eklenirken bir hata oluştu.";

            return RedirectToAction("Details", new { id = CarId });
        }

        public ActionResult Rent(int id)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Araç kiralamak için önce giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }
            var car = _carService.GetCarById(id);
            if (car == null || !car.IsAvailable) return HttpNotFound();
            return View(car);
        }

        [HttpPost]
        public ActionResult Rent(int carId, string firstName, string lastName, string email, string phone, DateTime rentDate, DateTime returnDate)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Araç kiralamak için önce giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }
            var carForPrice = _carService.GetCarById(carId);
            if (carForPrice == null) return HttpNotFound();
            decimal dailyPrice = carForPrice.DailyPrice;

            if (rentDate < DateTime.Today)
            {
                ViewBag.Error = "Kiralama tarihi geçmiş bir tarih olamaz.";
                return View(_carService.GetCarById(carId));
            }
            if (returnDate < rentDate)
            {
                ViewBag.Error = "Teslim tarihi, kiralama tarihinden önce olamaz.";
                return View(_carService.GetCarById(carId));
            }

            try
            {
                _carService.RentCar(carId, firstName, lastName, email, phone, rentDate, returnDate, dailyPrice);
                TempData["SuccessMessage"] = "Kiralama işlemi başarıyla tamamlandı!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(_carService.GetCarById(carId));
            }
        }
    }
}
