using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ArabamCarRental.Models
{
    public class Car
    {
        public int CarID { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string LicensePlate { get; set; }    
        public decimal DailyPrice { get; set; }
        public bool IsAvailable { get; set; }
        public string TechnicalSpecs { get; set; }
        public string ImageUrl { get; set; }
    }
}