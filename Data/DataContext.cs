using CarSharing.Models;
using Microsoft.EntityFrameworkCore;

namespace CarSharing.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Rents> Rents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Brand>().ToTable("Brand");
            modelBuilder.Entity<Rents>().ToTable("Rents");
            modelBuilder.Entity<Car>().ToTable("Car")
                .Property(c => c.Price)
                .HasColumnType("decimal(18, 2)");
        }
    }
}
