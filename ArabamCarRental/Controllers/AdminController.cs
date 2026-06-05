using ArabamCarRental.Models;
using ArabamCarRental.Services;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace ArabamCarRental.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarService _carService;

        public AdminController()
        {
            _carService = new CarService();
        }

        // ── Resim kaydetme yardımcı metodu ──
        private string SaveImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (Array.IndexOf(allowed, ext) < 0) return null;

            var folder = Server.MapPath("~/Content/CarImages/");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString("N") + ext;
            file.SaveAs(Path.Combine(folder, fileName));

            return "/Content/CarImages/" + fileName;
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            var adminUser = System.Configuration.ConfigurationManager.AppSettings["AdminUsername"];
            var adminPass = System.Configuration.ConfigurationManager.AppSettings["AdminPassword"];

            if (!string.IsNullOrEmpty(adminUser) && !string.IsNullOrEmpty(adminPass)
                && username == adminUser && password == adminPass)
            {
                Session["AdminLogin"] = true;
                return RedirectToAction("Index");
            }
            ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }

        public ActionResult Index()
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            var cars = _carService.GetAllCars();
            return View(cars);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        public ActionResult Create()
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            return View(new Car());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Car car, HttpPostedFileBase ImageFile, string TechVites, string TechYakit, string TechKoltuk, string TechBagaj, string TechKapi)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");

            if (_carService.IsLicensePlateExistForCreate(car.LicensePlate))
            {
                ViewBag.Error = $"Girdiğiniz '{car.LicensePlate.ToUpper()}' plakası sistemde başka bir araca ait! Lütfen benzersiz bir plaka girin.";
                return View(car);
            }

            car.TechnicalSpecs = $"{TechVites}, {TechYakit}, {TechKoltuk} Koltuk, {TechBagaj} Bagaj, {TechKapi} Kapı";

            var imageUrl = SaveImage(ImageFile);
            if (imageUrl != null) car.ImageUrl = imageUrl;

            _carService.AddCar(car);
            TempData["SuccessMessage"] = "Araç başarıyla eklendi.";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            var car = _carService.GetCarById(id);
            if (car == null) return HttpNotFound();
            return View(car);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Car car, HttpPostedFileBase ImageFile, string TechVites, string TechYakit, string TechKoltuk, string TechBagaj, string TechKapi)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");

            if (_carService.IsLicensePlateExistForEdit(car.LicensePlate, car.CarID))
            {
                ViewBag.Error = $"Girdiğiniz '{car.LicensePlate.ToUpper()}' plakası filodaki başka bir araç tarafından kullanılmaktadır!";
                return View(car);
            }

            car.TechnicalSpecs = $"{TechVites}, {TechYakit}, {TechKoltuk} Koltuk, {TechBagaj} Bagaj, {TechKapi} Kapı";

            var newImageUrl = SaveImage(ImageFile);
            if (newImageUrl != null)
            {
                car.ImageUrl = newImageUrl;
            }
            else if (string.IsNullOrEmpty(car.ImageUrl))
            {
                var existing = _carService.GetCarById(car.CarID);
                car.ImageUrl = existing?.ImageUrl;
            }

            _carService.UpdateCar(car);
            TempData["SuccessMessage"] = "Araç başarıyla güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            _carService.DeleteCar(id);
            TempData["SuccessMessage"] = "Araç silindi.";
            return RedirectToAction("Index");
        }

        public ActionResult Rentals()
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            var transactions = _carService.GetAllTransactions();
            return View(transactions);
        }

        public ActionResult Reviews()
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            var reviews = _carService.GetAllReviews();
            return View("AdminReviews", reviews);
        }

        [HttpPost]
        public ActionResult ApproveReview(int id)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            _carService.SetReviewApproval(id, true);
            TempData["SuccessMessage"] = "Yorum yayınlandı.";
            return RedirectToAction("Reviews");
        }

        [HttpPost]
        public ActionResult HideReview(int id)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            _carService.SetReviewApproval(id, false);
            TempData["SuccessMessage"] = "Yorum yayından kaldırıldı.";
            return RedirectToAction("Reviews");
        }

        public ActionResult Customers()
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            var customers = _carService.GetAllCustomers();
            return View("AdminCustomers", customers);
        }

        public ActionResult EditCustomer(int id)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            var customer = _carService.GetCustomerById(id);
            if (customer == null) return HttpNotFound();
            return PartialView("_EditCustomerModal", customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCustomer(Customer customer)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            _carService.UpdateCustomer(customer);
            TempData["SuccessMessage"] = "Müşteri bilgileri güncellendi.";
            return RedirectToAction("Customers");
        }

        [HttpPost]
        public ActionResult DeleteCustomer(int id)
        {
            if (Session["AdminLogin"] == null) return RedirectToAction("Login");
            _carService.DeleteCustomer(id);
            TempData["SuccessMessage"] = "Müşteri silindi.";
            return RedirectToAction("Customers");
        }
    }
}