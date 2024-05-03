using CarSharing.Models;
using Microsoft.IdentityModel.Tokens;

namespace CarSharing.Data
{
    public class Database
    {
        public static void Initialize(DataContext context)
        {
            // Check if any data exists in the database
            if (context.Brands.Any() || context.Users.Any())
            {
                // Database has been seeded
                return;
            }

            // Fill the database with data
            var brands = new Brand[]
             {
                new() { Name = "Toyota", },
                new() { Name = "Honda" },
                new() { Name = "Ford" },
                new() { Name = "Chevrolet" },
                new() { Name = "BMW" },
                new() { Name = "Mercedes-Benz" }
             };
            var users = new User[]
            {
                new() {
                    IsAdmin = true,
                    Login = "xoras",
                    Password = "123",
                    Name = "oskars",
                    Surname = "rascevskis",
                    Email = "sonar@example.com",
                    Address = "Saules iela 42",
                    PostalCode = "LV-5433"
                },
            };
            context.AddRange(brands);
            context.AddRange(users);
            context.SaveChanges();
        }
    }
}
