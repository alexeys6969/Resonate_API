using Microsoft.EntityFrameworkCore;
using Resonate_API.Models;
using System.Text;
using System.Security.Cryptography;

namespace Resonate_API.Classes
{
    public class DBManager : DbContext
    {
        public DbSet<Employees> Employees {  get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Sales> Sales { get; set; }
        public DbSet<Sale_Items> Sale_Items { get; set; }
        public DbSet<Suppliers> Suppliers {  get; set; }

        public DBManager() =>
            Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=127.0.0.1;port=3307;uid=root;pwd=;database=resonate", new MySqlServerVersion(new Version(8, 0, 11)));
        }

        public static string HashPassword(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
