using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Models;

namespace DemoMVC.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Quanlysinhvien> Quanlysinhviens { get; set; }

        public DbSet<Quanlymonhoc> Quanlymonhocs { get; set; }
    }
}