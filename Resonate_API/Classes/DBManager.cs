using Microsoft.EntityFrameworkCore;
using Resonate_API.Models;

namespace Resonate_API.Classes
{
    public class DBManager : DbContext
    {
        public DbSet<Employees> Employees {  get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DBManager() =>
            Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=127.0.0.1;port=3307;uid=root;pwd=;database=resonate", new MySqlServerVersion(new Version(8, 0, 11)));
        }
    }
}
