using System;
using System.Configuration;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using ArabamCarRental.Models;

namespace ArabamCarRental.Services
{
    public class AccountService
    {
        private readonly string _connectionString;

        public AccountService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ArabamCarRentalContext"].ConnectionString;
        }

        public static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private void EnsurePasswordColumns(SQLiteConnection conn)
        {
            try { using (var c = new SQLiteCommand("ALTER TABLE Customers ADD COLUMN PasswordResetToken TEXT", conn)) c.ExecuteNonQuery(); } catch { }
            try { using (var c = new SQLiteCommand("ALTER TABLE Customers ADD COLUMN PasswordResetExpiry TEXT", conn)) c.ExecuteNonQuery(); } catch { }
        }

        // ── Giriş 
        public Customer Login(string email, string password)
        {
            Customer customer = null;
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                EnsurePasswordColumns(conn);

                string hashedPassword = HashPassword(password);
                string query = @"SELECT CustomerID, FirstName, LastName, Email, Password
                                 FROM Customers WHERE Email = @Email";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPassword = reader["Password"]?.ToString() ?? "";
                            bool match = storedPassword == hashedPassword || storedPassword == password;
                            if (match)
                            {
                                customer = new Customer
                                {
                                    CustomerID = Convert.ToInt32(reader["CustomerID"]),
                                    FirstName  = reader["FirstName"].ToString(),
                                    LastName   = reader["LastName"].ToString(),
                                    Email      = reader["Email"].ToString()
                                };
                                if (storedPassword == password && storedPassword != hashedPassword)
                                {
                                    UpgradePasswordHash(customer.CustomerID, hashedPassword, conn);
                                }
                            }
                        }
                    }
                }
            }
            return customer;
        }

        private void UpgradePasswordHash(int customerId, string hashedPassword, SQLiteConnection conn)
        {
            try
            {
                using (var cmd = new SQLiteCommand(
                    "UPDATE Customers SET Password = @P WHERE CustomerID = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@P", hashedPassword);
                    cmd.Parameters.AddWithValue("@ID", customerId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        // ── Kayıt 
        public void Register(Customer customer)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                EnsurePasswordColumns(conn);

                string hashedPassword = HashPassword(customer.Password);
                string query = "INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber, Password) VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @Password)";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName",   customer.FirstName);
                    cmd.Parameters.AddWithValue("@LastName",    customer.LastName);
                    cmd.Parameters.AddWithValue("@Email",       customer.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", (object)customer.PhoneNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password",    hashedPassword);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── E-posta kontrolü 
        public bool EmailExists(string email)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM Customers WHERE Email = @Email";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value && Convert.ToInt64(result) > 0;
                }
            }
        }

        // ── Şifre Sıfırlama

        public string GeneratePasswordResetToken(string email)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                EnsurePasswordColumns(conn);

                string checkQuery = "SELECT CustomerID FROM Customers WHERE Email = @Email";
                int customerId = 0;
                using (SQLiteCommand cmd = new SQLiteCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    object result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return null;
                    customerId = Convert.ToInt32(result);
                }

                string token = GenerateSecureToken();
                DateTime expiry = DateTime.Now.AddHours(1);

                string updateQuery = @"UPDATE Customers
                                       SET PasswordResetToken  = @Token,
                                           PasswordResetExpiry = @Expiry
                                       WHERE CustomerID = @ID";
                using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Token",  token);
                    cmd.Parameters.AddWithValue("@Expiry", expiry.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@ID",     customerId);
                    cmd.ExecuteNonQuery();
                }

                return token;
            }
        }

        // ── Şifre Sıfırlama: 
        public int ValidatePasswordResetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return -1;

            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                EnsurePasswordColumns(conn);

                string query = @"SELECT CustomerID, PasswordResetExpiry
                                 FROM Customers
                                 WHERE PasswordResetToken = @Token";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return -1;

                        int id = Convert.ToInt32(reader["CustomerID"]);
                        string expiryStr = reader["PasswordResetExpiry"]?.ToString();
                        if (string.IsNullOrEmpty(expiryStr)) return -1;

                        DateTime expiry = Convert.ToDateTime(expiryStr);
                        if (DateTime.Now > expiry) return -1; 

                        return id;
                    }
                }
            }
        }

        // ── Şifre Sıfırlama: Yeni Şifreyi Kaydet 
        public bool ResetPassword(string token, string newPassword)
        {
            int customerId = ValidatePasswordResetToken(token);
            if (customerId == -1) return false;

            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string hashedPassword = HashPassword(newPassword);
                string updateQuery = @"UPDATE Customers
                                       SET Password             = @Password,
                                           PasswordResetToken  = NULL,
                                           PasswordResetExpiry = NULL
                                       WHERE CustomerID = @ID";
                using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@ID",       customerId);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        // ── Yardımcı: güvenli token üret 
        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                          .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
