using ArabamCarRental.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;

namespace ArabamCarRental.Services
{
    public class CarService
    {
        private readonly string _connectionString;

        public CarService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ArabamCarRentalContext"].ConnectionString;
        }

        // --- 1. MÜŞTERI HESAP İŞLEMLERİ ---

        public void AddCustomer(Customer customer)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber, Password) VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @Password)";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", customer.LastName);
                    cmd.Parameters.AddWithValue("@Email", customer.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                    cmd.Parameters.AddWithValue("@Password", (object)customer.Password ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Customer LoginCustomer(string email, string password)
        {
            Customer customer = null;
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "SELECT CustomerID, FirstName, LastName, Email FROM Customers WHERE Email = @Email AND Password = @Password";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);

                    conn.Open();
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customer = new Customer
                            {
                                CustomerID = Convert.ToInt32(reader["CustomerID"]),
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Email = reader["Email"].ToString()
                            };
                        }
                    }
                }
            }
            return customer;
        }

        // --- 2. ARAÇ LİSTELEME VE DETAY İŞLEMLERİ ---

        public List<Car> GetAllCars(string brand = null, decimal? minPrice = null, decimal? maxPrice = null, string vites = null, string yakit = null)
        {
            List<Car> cars = new List<Car>();

            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "SELECT CarID, Brand, Model, LicensePlate, DailyPrice, IsAvailable, TechnicalSpecs, ImageUrl FROM Cars WHERE 1=1";

                if (!string.IsNullOrEmpty(brand)) query += " AND Brand = @Brand";
                if (minPrice.HasValue) query += " AND DailyPrice >= @MinPrice";
                if (maxPrice.HasValue) query += " AND DailyPrice <= @MaxPrice";
                if (!string.IsNullOrEmpty(vites)) query += " AND TechnicalSpecs LIKE @Vites";
                if (!string.IsNullOrEmpty(yakit)) query += " AND TechnicalSpecs LIKE @Yakit";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(brand)) cmd.Parameters.AddWithValue("@Brand", brand);
                    if (minPrice.HasValue) cmd.Parameters.AddWithValue("@MinPrice", minPrice.Value);
                    if (maxPrice.HasValue) cmd.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);
                    if (!string.IsNullOrEmpty(vites)) cmd.Parameters.AddWithValue("@Vites", "%" + vites + "%");
                    if (!string.IsNullOrEmpty(yakit)) cmd.Parameters.AddWithValue("@Yakit", "%" + yakit + "%");

                    conn.Open();
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cars.Add(new Car
                            {
                                CarID = Convert.ToInt32(reader["CarID"]),
                                Brand = reader["Brand"].ToString(),
                                Model = reader["Model"].ToString(),
                                LicensePlate = reader["LicensePlate"]?.ToString(),
                                DailyPrice = Convert.ToDecimal(reader["DailyPrice"]),
                                IsAvailable = Convert.ToBoolean(reader["IsAvailable"]),
                                TechnicalSpecs = reader["TechnicalSpecs"]?.ToString(),
                                ImageUrl = reader["ImageUrl"]?.ToString()
                            });
                        }
                    }
                }
            }
            return cars;
        }

        public List<string> GetDistinctBrands()
        {
            var brands = new List<string>();
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "SELECT DISTINCT Brand FROM Cars ORDER BY Brand";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    conn.Open();
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            brands.Add(reader["Brand"].ToString());
                    }
                }
            }
            return brands;
        }

        public Car GetCarById(int carId)
        {
            Car car = null;
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "SELECT CarID, Brand, Model, LicensePlate, DailyPrice, IsAvailable, TechnicalSpecs, ImageUrl FROM Cars WHERE CarID = @CarID";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CarID", carId);
                    conn.Open();
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            car = new Car
                            {
                                CarID = Convert.ToInt32(reader["CarID"]),
                                Brand = reader["Brand"].ToString(),
                                Model = reader["Model"].ToString(),
                                LicensePlate = reader["LicensePlate"]?.ToString(),
                                DailyPrice = Convert.ToDecimal(reader["DailyPrice"]),
                                IsAvailable = Convert.ToBoolean(reader["IsAvailable"]),
                                TechnicalSpecs = reader["TechnicalSpecs"]?.ToString(),
                                ImageUrl = reader["ImageUrl"]?.ToString()
                            };
                        }
                    }
                }
            }
            return car;
        }

        // --- PLAKA BENZERSİZLİK KONTROLÜ METOTLARI ---

        public bool IsLicensePlateExistForCreate(string licensePlate)
        {
            if (string.IsNullOrEmpty(licensePlate)) return false;

            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM Cars WHERE UPPER(TRIM(LicensePlate)) = UPPER(TRIM(@LicensePlate))";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LicensePlate", licensePlate);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public bool IsLicensePlateExistForEdit(string licensePlate, int currentCarId)
        {
            if (string.IsNullOrEmpty(licensePlate)) return false;

            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM Cars WHERE UPPER(TRIM(LicensePlate)) = UPPER(TRIM(@LicensePlate)) AND CarID != @CarID";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LicensePlate", licensePlate);
                    cmd.Parameters.AddWithValue("@CarID", currentCarId);
                    conn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        // --- 3. KİRALAMA İŞLEMİ (TRANSACTIONS) ---

        private void EnsureIsCancelledColumn(SQLiteConnection conn)
        {
            try
            {
                using (var c = new SQLiteCommand(
                    "ALTER TABLE Transactions ADD COLUMN IsCancelled INTEGER NOT NULL DEFAULT 0", conn))
                    c.ExecuteNonQuery();
            }
            catch { }
        }

        public void RentCar(int carId, string firstName, string lastName, string email, string phone, DateTime rentDate, DateTime returnDate, decimal dailyPrice)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (SQLiteTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int customerId;
                        string checkCustomerQuery = "SELECT CustomerID FROM Customers WHERE Email = @Email";
                        using (SQLiteCommand cmdCheck = new SQLiteCommand(checkCustomerQuery, conn, transaction))
                        {
                            cmdCheck.Parameters.AddWithValue("@Email", email);
                            object result = cmdCheck.ExecuteScalar();

                            if (result != null)
                            {
                                customerId = Convert.ToInt32(result);
                            }
                            else
                            {
                                string insertCustomerQuery = "INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber, Password) VALUES (@FirstName, @LastName, @Email, @Phone, @Password)";
                                using (SQLiteCommand cmdInsertCust = new SQLiteCommand(insertCustomerQuery, conn, transaction))
                                {
                                    cmdInsertCust.Parameters.AddWithValue("@FirstName", firstName);
                                    cmdInsertCust.Parameters.AddWithValue("@LastName", lastName);
                                    cmdInsertCust.Parameters.AddWithValue("@Email", email);
                                    cmdInsertCust.Parameters.AddWithValue("@Phone", phone);
                                    cmdInsertCust.Parameters.AddWithValue("@Password", DBNull.Value);
                                    cmdInsertCust.ExecuteNonQuery();
                                }
                                using (SQLiteCommand cmdLastId = new SQLiteCommand("SELECT last_insert_rowid()", conn, transaction))
                                {
                                    customerId = Convert.ToInt32(cmdLastId.ExecuteScalar());
                                }
                            }
                        }

                        int totalDays = (returnDate - rentDate).Days;
                        if (totalDays == 0) totalDays = 1;
                        decimal totalPrice = totalDays * dailyPrice;

                        string insertTransactionQuery = "INSERT INTO Transactions (CarID, CustomerID, RentDate, ReturnDate, TotalPrice) VALUES (@CarID, @CustomerID, @RentDate, @ReturnDate, @TotalPrice)";
                        using (SQLiteCommand cmdTrans = new SQLiteCommand(insertTransactionQuery, conn, transaction))
                        {
                            cmdTrans.Parameters.AddWithValue("@CarID", carId);
                            cmdTrans.Parameters.AddWithValue("@CustomerID", customerId);
                            cmdTrans.Parameters.AddWithValue("@RentDate", rentDate);
                            cmdTrans.Parameters.AddWithValue("@ReturnDate", returnDate);
                            cmdTrans.Parameters.AddWithValue("@TotalPrice", totalPrice);
                            cmdTrans.ExecuteNonQuery();
                        }

                        string updateCarQuery = "UPDATE Cars SET IsAvailable = 0 WHERE CarID = @CarID";
                        using (SQLiteCommand cmdUpdateCar = new SQLiteCommand(updateCarQuery, conn, transaction))
                        {
                            cmdUpdateCar.Parameters.AddWithValue("@CarID", carId);
                            cmdUpdateCar.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Kiralama işlemi sırasında bir hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        public List<TransactionViewModel> GetAllTransactions()
        {
            List<TransactionViewModel> transactions = new List<TransactionViewModel>();
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                EnsureIsCancelledColumn(conn);

                string query = @"
                    SELECT
                        t.TransactionID,
                        t.CarID,
                        c.Brand || ' ' || c.Model AS CarInfo,
                        COALESCE(cust.FirstName || ' ' || cust.LastName, '(Silinmiş Müşteri)') AS CustomerInfo,
                        COALESCE(cust.Email, '-') AS CustomerEmail,
                        COALESCE(cust.PhoneNumber, '-') AS CustomerPhone,
                        t.RentDate,
                        t.ReturnDate,
                        t.TotalPrice,
                        c.ImageUrl,
                        COALESCE(t.IsCancelled, 0) AS IsCancelled,
                        cust.CustomerID AS CustomerExists
                    FROM Transactions t
                    INNER JOIN Cars c ON t.CarID = c.CarID
                    LEFT JOIN Customers cust ON t.CustomerID = cust.CustomerID
                    ORDER BY t.RentDate DESC";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rentDate = Convert.ToDateTime(reader["RentDate"]);
                            var returnDate = Convert.ToDateTime(reader["ReturnDate"]);
                            var isCancelled = Convert.ToBoolean(reader["IsCancelled"]);
                            var customerExists = reader["CustomerExists"] != DBNull.Value;

                            transactions.Add(new TransactionViewModel
                            {
                                TransactionID = Convert.ToInt32(reader["TransactionID"]),
                                CarID = Convert.ToInt32(reader["CarID"]),
                                CarInfo = reader["CarInfo"].ToString(),
                                CustomerInfo = reader["CustomerInfo"].ToString(),
                                CustomerEmail = reader["CustomerEmail"].ToString(),
                                CustomerPhone = reader["CustomerPhone"].ToString(),
                                RentDate = rentDate,
                                ReturnDate = returnDate,
                                TotalPrice = Convert.ToDecimal(reader["TotalPrice"]),
                                CarImageUrl = reader["ImageUrl"]?.ToString(),
                                TotalDays = (returnDate - rentDate).Days == 0 ? 1 : (returnDate - rentDate).Days,
                                IsCancelled = isCancelled,
                                IsActive = !isCancelled && customerExists && returnDate.Date >= DateTime.Today
                            });
                        }
                    }
                }
            }
            return transactions;
        }

        // --- 4. YÖNETİCİ (ADMIN) CRUD İŞLEMLERİ ---

        public void AddCar(Car car)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = @"INSERT INTO Cars (Brand, Model, LicensePlate, DailyPrice, IsAvailable, TechnicalSpecs, ImageUrl) 
                                 VALUES (@Brand, @Model, @LicensePlate, @DailyPrice, @IsAvailable, @TechnicalSpecs, @ImageUrl)";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Brand", car.Brand);
                    cmd.Parameters.AddWithValue("@Model", car.Model);
                    cmd.Parameters.AddWithValue("@LicensePlate", (object)car.LicensePlate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DailyPrice", car.DailyPrice);
                    cmd.Parameters.AddWithValue("@IsAvailable", car.IsAvailable);
                    cmd.Parameters.AddWithValue("@TechnicalSpecs", (object)car.TechnicalSpecs ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImageUrl", (object)car.ImageUrl ?? DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateCar(Car car)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = @"UPDATE Cars 
                                 SET Brand = @Brand, Model = @Model, LicensePlate = @LicensePlate, DailyPrice = @DailyPrice, 
                                     IsAvailable = @IsAvailable, TechnicalSpecs = @TechnicalSpecs, ImageUrl = @ImageUrl 
                                 WHERE CarID = @CarID";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarID);
                    cmd.Parameters.AddWithValue("@Brand", car.Brand);
                    cmd.Parameters.AddWithValue("@Model", car.Model);
                    cmd.Parameters.AddWithValue("@LicensePlate", (object)car.LicensePlate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DailyPrice", car.DailyPrice);
                    cmd.Parameters.AddWithValue("@IsAvailable", car.IsAvailable);
                    cmd.Parameters.AddWithValue("@TechnicalSpecs", (object)car.TechnicalSpecs ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImageUrl", (object)car.ImageUrl ?? DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteCar(int carId)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "DELETE FROM Cars WHERE CarID = @CarID";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CarID", carId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // --- ARAÇ YORUM İŞLEMLERİ ---

        private void EnsureReviewsTable(SQLiteConnection conn)
        {
            string createTable = @"CREATE TABLE IF NOT EXISTS CarReviews (
                                       ReviewID   INTEGER PRIMARY KEY AUTOINCREMENT,
                                       CarID      INTEGER NOT NULL,
                                       CustomerID INTEGER NOT NULL,
                                       Rating     INTEGER NOT NULL CHECK (Rating BETWEEN 1 AND 5),
                                       Comment    TEXT NOT NULL,
                                       PhotoUrl   TEXT,
                                       IsApproved INTEGER NOT NULL DEFAULT 1,
                                       ReviewDate TEXT NOT NULL
                                   )";
            using (var cmd = new SQLiteCommand(createTable, conn)) cmd.ExecuteNonQuery();

            try { using (var c = new SQLiteCommand("ALTER TABLE CarReviews ADD COLUMN PhotoUrl TEXT", conn)) c.ExecuteNonQuery(); } catch { }
            try { using (var c = new SQLiteCommand("ALTER TABLE CarReviews ADD COLUMN IsApproved INTEGER NOT NULL DEFAULT 1", conn)) c.ExecuteNonQuery(); } catch { }

            string createIndex = @"CREATE UNIQUE INDEX IF NOT EXISTS idx_review_car_customer ON CarReviews(CarID, CustomerID)";
            using (var idxCmd = new SQLiteCommand(createIndex, conn)) idxCmd.ExecuteNonQuery();
        }

        public List<CarReview> GetReviewsByCarId(int carId)
        {
            var reviews = new List<CarReview>();
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    EnsureReviewsTable(conn);

                    string query = @"SELECT r.ReviewID, r.CarID, r.CustomerID, r.Rating, r.Comment,
                                            r.PhotoUrl, r.IsApproved, r.ReviewDate,
                                            c.FirstName || ' ' || c.LastName AS ReviewerName,
                                            SUBSTR(c.FirstName,1,1) || SUBSTR(c.LastName,1,1) AS ReviewerInitials
                                     FROM CarReviews r
                                     INNER JOIN Customers c ON r.CustomerID = c.CustomerID
                                     WHERE r.CarID = @CarID AND r.IsApproved = 1
                                     ORDER BY r.ReviewDate DESC";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CarID", carId);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                reviews.Add(new CarReview
                                {
                                    ReviewID = Convert.ToInt32(reader["ReviewID"]),
                                    CarID = Convert.ToInt32(reader["CarID"]),
                                    CustomerID = Convert.ToInt32(reader["CustomerID"]),
                                    Rating = Convert.ToInt32(reader["Rating"]),
                                    Comment = reader["Comment"].ToString(),
                                    PhotoUrl = reader["PhotoUrl"]?.ToString(),
                                    IsApproved = Convert.ToBoolean(reader["IsApproved"]),
                                    ReviewDate = Convert.ToDateTime(reader["ReviewDate"]),
                                    ReviewerName = reader["ReviewerName"].ToString(),
                                    ReviewerInitials = reader["ReviewerInitials"].ToString().ToUpper()
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return reviews;
        }

        public List<CarReview> GetAllReviews()
        {
            var reviews = new List<CarReview>();
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    EnsureReviewsTable(conn);

                    string query = @"SELECT r.ReviewID, r.CarID, r.CustomerID, r.Rating, r.Comment,
                                            r.PhotoUrl, r.IsApproved, r.ReviewDate,
                                            c.FirstName || ' ' || c.LastName AS ReviewerName,
                                            ca.Brand || ' ' || ca.Model AS CarInfo
                                     FROM CarReviews r
                                     INNER JOIN Customers c ON r.CustomerID = c.CustomerID
                                     INNER JOIN Cars ca ON r.CarID = ca.CarID
                                     ORDER BY r.ReviewDate DESC";
                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rv = new CarReview
                            {
                                ReviewID = Convert.ToInt32(reader["ReviewID"]),
                                CarID = Convert.ToInt32(reader["CarID"]),
                                CustomerID = Convert.ToInt32(reader["CustomerID"]),
                                Rating = Convert.ToInt32(reader["Rating"]),
                                Comment = reader["Comment"].ToString(),
                                PhotoUrl = reader["PhotoUrl"]?.ToString(),
                                IsApproved = Convert.ToBoolean(reader["IsApproved"]),
                                ReviewDate = Convert.ToDateTime(reader["ReviewDate"]),
                                ReviewerName = reader["ReviewerName"].ToString(),
                                CarInfo = reader["CarInfo"].ToString()
                            };
                            reviews.Add(rv);
                        }
                    }
                }
            }
            catch { }
            return reviews;
        }

        public bool SetReviewApproval(int reviewId, bool approved)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string q = "UPDATE CarReviews SET IsApproved = @A WHERE ReviewID = @ID";
                    using (SQLiteCommand cmd = new SQLiteCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@A", approved ? 1 : 0);
                        cmd.Parameters.AddWithValue("@ID", reviewId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public List<Customer> GetAllCustomers()
        {
            var list = new List<Customer>();
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string q = "SELECT CustomerID, FirstName, LastName, Email, PhoneNumber FROM Customers ORDER BY CustomerID DESC";
                    using (SQLiteCommand cmd = new SQLiteCommand(q, conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Customer
                            {
                                CustomerID = Convert.ToInt32(reader["CustomerID"]),
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Email = reader["Email"].ToString(),
                                PhoneNumber = reader["PhoneNumber"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public bool AddReview(int carId, int customerId, int rating, string comment, string photoUrl = null)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    EnsureReviewsTable(conn);

                    string upsertQuery = @"INSERT INTO CarReviews (CarID, CustomerID, Rating, Comment, PhotoUrl, IsApproved, ReviewDate)
                                           VALUES (@CarID, @CustomerID, @Rating, @Comment, @PhotoUrl, 1, datetime('now'))
                                           ON CONFLICT(CarID, CustomerID) DO UPDATE
                                           SET Rating=excluded.Rating, Comment=excluded.Comment,
                                               PhotoUrl=excluded.PhotoUrl, ReviewDate=excluded.ReviewDate";

                    using (SQLiteCommand cmd = new SQLiteCommand(upsertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@CarID", carId);
                        cmd.Parameters.AddWithValue("@CustomerID", customerId);
                        cmd.Parameters.AddWithValue("@Rating", rating);
                        cmd.Parameters.AddWithValue("@Comment", comment);
                        cmd.Parameters.AddWithValue("@PhotoUrl", (object)photoUrl ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public static string GetCorrectImageUrl(string brand, string model, string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                if (imageUrl.StartsWith("/Content/CarImages/"))
                {
                    return imageUrl;
                }
                return "/Content/CarImages/" + imageUrl;
            }
            return null;
        }

        // --- 5. KULLANICI KİRALAMA GEÇMİŞİ ---

        public List<TransactionViewModel> GetTransactionsByCustomerId(int customerId)
        {
            var list = new List<TransactionViewModel>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                EnsureIsCancelledColumn(conn);

                string query = @"
                    SELECT
                        t.TransactionID,
                        t.CarID,
                        c.Brand || ' ' || c.Model AS CarInfo,
                        t.RentDate,
                        t.ReturnDate,
                        t.TotalPrice,
                        c.ImageUrl,
                        COALESCE(t.IsCancelled, 0) AS IsCancelled
                    FROM Transactions t
                    INNER JOIN Cars c ON t.CarID = c.CarID
                    WHERE t.CustomerID = @CustomerID
                    ORDER BY t.RentDate DESC";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerID", customerId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rentDate = Convert.ToDateTime(reader["RentDate"]);
                            var returnDate = Convert.ToDateTime(reader["ReturnDate"]);
                            var isCancelled = Convert.ToBoolean(reader["IsCancelled"]);
                            list.Add(new TransactionViewModel
                            {
                                TransactionID = Convert.ToInt32(reader["TransactionID"]),
                                CarID = Convert.ToInt32(reader["CarID"]),
                                CarInfo = reader["CarInfo"].ToString(),
                                RentDate = rentDate,
                                ReturnDate = returnDate,
                                TotalPrice = Convert.ToDecimal(reader["TotalPrice"]),
                                CarImageUrl = reader["ImageUrl"]?.ToString(),
                                TotalDays = (returnDate - rentDate).Days == 0 ? 1 : (returnDate - rentDate).Days,
                                IsCancelled = isCancelled,
                                IsActive = !isCancelled && returnDate.Date >= DateTime.Today
                            });
                        }
                    }
                }
            }
            return list;
        }

        public bool CancelRental(int transactionId, int customerId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                EnsureIsCancelledColumn(conn);

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string checkQuery = @"SELECT CarID, ReturnDate, COALESCE(IsCancelled,0) AS IsCancelled
                                              FROM Transactions
                                              WHERE TransactionID = @TID AND CustomerID = @CID";
                        int carId = 0;
                        DateTime returnDate = DateTime.MinValue;
                        using (var cmd = new SQLiteCommand(checkQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TID", transactionId);
                            cmd.Parameters.AddWithValue("@CID", customerId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read()) return false;
                                if (Convert.ToBoolean(reader["IsCancelled"])) return false;
                                carId = Convert.ToInt32(reader["CarID"]);
                                returnDate = Convert.ToDateTime(reader["ReturnDate"]);
                            }
                        }
                        if (returnDate.Date < DateTime.Today) return false;

                        string cancelQuery = "UPDATE Transactions SET IsCancelled = 1 WHERE TransactionID = @TID";
                        using (var cmd = new SQLiteCommand(cancelQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TID", transactionId);
                            cmd.ExecuteNonQuery();
                        }

                        string updateCar = "UPDATE Cars SET IsAvailable = 1 WHERE CarID = @CarID";
                        using (var cmd = new SQLiteCommand(updateCar, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CarID", carId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool UpdateCustomer(Customer customer)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string q = @"UPDATE Customers SET FirstName=@FirstName, LastName=@LastName,
                                 Email=@Email, PhoneNumber=@PhoneNumber WHERE CustomerID=@CustomerID";
                    using (SQLiteCommand cmd = new SQLiteCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", customer.LastName);
                        cmd.Parameters.AddWithValue("@Email", customer.Email);
                        cmd.Parameters.AddWithValue("@PhoneNumber", (object)customer.PhoneNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerID", customer.CustomerID);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public bool DeleteCustomer(int customerId)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Customers WHERE CustomerID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", customerId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public Customer GetCustomerById(int customerId)
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string q = "SELECT CustomerID, FirstName, LastName, Email, PhoneNumber FROM Customers WHERE CustomerID=@ID";
                    using (SQLiteCommand cmd = new SQLiteCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", customerId);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                                return new Customer
                                {
                                    CustomerID = Convert.ToInt32(reader["CustomerID"]),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    PhoneNumber = reader["PhoneNumber"]?.ToString()
                                };
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}