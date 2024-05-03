using CarSharing.Models;
using Microsoft.EntityFrameworkCore;

namespace CarSharing.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        // Define tables in database
        public DbSet<User> Users { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Rents> Rents { get; set; }
        public DbSet<RatingAndReview> RatingAndReviews { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }

        // Add tables to database
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Brand>().ToTable("Brand");
            modelBuilder.Entity<Rents>().ToTable("Rents");
            modelBuilder.Entity<Car>().ToTable("Car")
                .Property(c => c.Price)
                .HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<RatingAndReview>().ToTable("RatingAndReview");
            modelBuilder.Entity<Chat>().ToTable("Chat");
            modelBuilder.Entity<Message>().ToTable("Message");
        }
    }
}
