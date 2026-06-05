using System.Configuration;
using System.Data.SQLite;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace ArabamCarRental
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string connStr = ConfigurationManager.ConnectionStrings["ArabamCarRentalContext"].ConnectionString;
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();

                new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS Cars (
                    CarID        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Brand        TEXT NOT NULL,
                    Model        TEXT NOT NULL,
                    LicensePlate TEXT NOT NULL UNIQUE,
                    DailyPrice   REAL NOT NULL,
                    IsAvailable  INTEGER NOT NULL DEFAULT 1,
                    TechnicalSpecs TEXT,
                    ImageUrl     TEXT
                )", conn).ExecuteNonQuery();

                new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS Customers (
                    CustomerID  INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName   TEXT NOT NULL,
                    LastName    TEXT NOT NULL,
                    Email       TEXT NOT NULL UNIQUE,
                    PhoneNumber TEXT,
                    Password    TEXT NOT NULL,
                    PasswordResetToken  TEXT,
                    PasswordResetExpiry TEXT
                )", conn).ExecuteNonQuery();

                new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS Transactions (
                    TransactionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    CarID         INTEGER NOT NULL,
                    CustomerID    INTEGER,
                    RentDate      TEXT NOT NULL,
                    ReturnDate    TEXT NOT NULL,
                    TotalPrice    REAL NOT NULL,
                    IsCancelled   INTEGER NOT NULL DEFAULT 0
                )", conn).ExecuteNonQuery();

                new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS CarReviews (
                    ReviewID   INTEGER PRIMARY KEY AUTOINCREMENT,
                    CarID      INTEGER NOT NULL,
                    CustomerID INTEGER NOT NULL,
                    Rating     INTEGER NOT NULL,
                    Comment    TEXT,
                    ReviewDate TEXT NOT NULL,
                    IsApproved INTEGER NOT NULL DEFAULT 0,
                    UNIQUE(CarID, CustomerID)
                )", conn).ExecuteNonQuery();
            }
        }
    }
}