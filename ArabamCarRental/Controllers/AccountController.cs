using System;
using System.Web;
using System.Web.Mvc;
using ArabamCarRental.Models;
using ArabamCarRental.Services;

namespace ArabamCarRental.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService = new AccountService();

        // ── Giriş 

        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserId"] != null)
                return RedirectToAction("Index", "Home");

            var emailCookie = Request.Cookies["RememberEmail"];
            if (emailCookie != null)
                ViewBag.RememberedEmail = emailCookie.Value;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Email, string Password, bool RememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["ErrorMessage"] = "E-posta ve şifre boş bırakılamaz.";
                return View();
            }

            var customer = _accountService.Login(Email.Trim(), Password);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "E-posta adresi veya şifre hatalı.";
                return View();
            }

            if (RememberMe)
            {
                var cookie = new HttpCookie("RememberEmail", customer.Email)
                {
                    Expires  = DateTime.Now.AddDays(30),
                    HttpOnly = true
                };
                Response.Cookies.Add(cookie);
            }
            else
            {
                if (Request.Cookies["RememberEmail"] != null)
                {
                    var expiredCookie = new HttpCookie("RememberEmail") { Expires = DateTime.Now.AddDays(-1) };
                    Response.Cookies.Add(expiredCookie);
                }
            }

            Session["UserId"]    = customer.CustomerID;
            Session["UserName"]  = customer.FirstName + " " + customer.LastName;
            Session["UserEmail"] = customer.Email;

            TempData["SuccessMessage"] = "Hoş geldiniz, " + customer.FirstName + "!";
            return RedirectToAction("Index", "Home");
        }

        // ── Kayıt 

        [HttpGet]
        public ActionResult Register()
        {
            if (Session["UserId"] != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string FirstName, string LastName,
                                     string Email, string PhoneNumber,
                                     string Password, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName)
                || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["ErrorMessage"] = "Lütfen tüm zorunlu alanları doldurun.";
                return View();
            }

            if (Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Şifreler eşleşmiyor.";
                return View();
            }

            if (Password.Length < 6)
            {
                TempData["ErrorMessage"] = "Şifre en az 6 karakter olmalıdır.";
                return View();
            }

            if (_accountService.EmailExists(Email.Trim()))
            {
                TempData["ErrorMessage"] = "Bu e-posta adresi zaten kayıtlı.";
                return View();
            }

            var customer = new Customer
            {
                FirstName   = FirstName.Trim(),
                LastName    = LastName.Trim(),
                Email       = Email.Trim().ToLower(),
                PhoneNumber = PhoneNumber?.Trim(),
                Password    = Password
            };

            _accountService.Register(customer);

            TempData["SuccessMessage"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Login", "Account");
        }

        // ── Şifre Sıfırlama 

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            if (Session["UserId"] != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                TempData["ErrorMessage"] = "Lütfen e-posta adresinizi girin.";
                return View();
            }

            string token = _accountService.GeneratePasswordResetToken(Email.Trim().ToLower());

            TempData["SuccessMessage"] = "E-posta adresiniz kayıtlıysa şifre sıfırlama bağlantısı gönderildi.";

            if (token != null)
            {
                string resetLink = Url.Action("ResetPassword", "Account",
                                              new { token = token }, Request.Url.Scheme);
                TempData["DevResetLink"] = resetLink;
            }

            return RedirectToAction("ForgotPassword");
        }

        // ── Şifre Sıfırlama: Yeni Şifre 

        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login");

            int customerId = _accountService.ValidatePasswordResetToken(token);
            if (customerId == -1)
            {
                TempData["ErrorMessage"] = "Bu şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string token, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Şifre en az 6 karakter olmalıdır.";
                ViewBag.Token = token;
                return View();
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Şifreler eşleşmiyor.";
                ViewBag.Token = token;
                return View();
            }

            bool success = _accountService.ResetPassword(token, NewPassword);
            if (!success)
            {
                TempData["ErrorMessage"] = "Bu şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
                return RedirectToAction("ForgotPassword");
            }

            TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }

        // ── Kiralamalarım 

        public ActionResult MyRentals()
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Bu sayfayı görüntülemek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }
            int customerId = Convert.ToInt32(Session["UserId"]);
            var carService = new CarService();
            var rentals = carService.GetTransactionsByCustomerId(customerId);
            return View(rentals);
        }

        // ── Kiralama İptal 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelRental(int transactionId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int customerId = Convert.ToInt32(Session["UserId"]);
            var carService = new CarService();
            bool ok = carService.CancelRental(transactionId, customerId);

            if (ok)
                TempData["SuccessMessage"] = "Kiralama başarıyla iptal edildi.";
            else
                TempData["ErrorMessage"] = "İptal işlemi gerçekleştirilemedi.";

            return RedirectToAction("MyRentals");
        }

        // ── Çıkış 

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            TempData["SuccessMessage"] = "Başarıyla çıkış yapıldı.";
            return RedirectToAction("Login", "Account");
        }
    }
}
