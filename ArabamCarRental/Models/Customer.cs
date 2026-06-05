using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ArabamCarRental.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }

        // Şifre sıfırlama
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
    }
}
