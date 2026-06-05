using System;

namespace ArabamCarRental.Models
{
    public class TransactionViewModel
    {
        public int TransactionID { get; set; }
        public string CarInfo { get; set; }
        public string CustomerInfo { get; set; }
        public string CustomerPhone { get; set; }
        public DateTime RentDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal TotalPrice { get; set; }

        public int CarID { get; set; }
        public string CustomerEmail { get; set; }
        public string CarImageUrl { get; set; }
        public bool IsActive { get; set; }     
        public bool IsCancelled { get; set; }   
        public int TotalDays { get; set; }
    }
}
