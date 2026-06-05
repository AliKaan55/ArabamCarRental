using System;

namespace ArabamCarRental.Models
{
    public class CarReview
    {
        public int ReviewID { get; set; }
        public int CarID { get; set; }
        public int CustomerID { get; set; }
        public string ReviewerName { get; set; }
        public string ReviewerInitials { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string PhotoUrl { get; set; }   // fotoğraflı yorum
        public bool IsApproved { get; set; }   // admin onayı
        public string CarInfo { get; set; }    // admin listesi için
        public DateTime ReviewDate { get; set; }
    }
}
