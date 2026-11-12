using Microsoft.EntityFrameworkCore;
using DemoMVC.Models;

namespace DemoMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Quanlysinhvien> Quanlysinhviens { get; set; }

        public DbSet<Quanlymonhoc> Quanlymonhocs { get; set; }
    }
}