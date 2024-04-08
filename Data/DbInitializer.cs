using CarSharing.Models;
using Microsoft.IdentityModel.Tokens;

namespace CarSharing.Data
{
    public class DbInitializer
    {
        public static void Initialize(DataContext context)
        {
            // Check if any data exists in the database
            if (context.Brands.Any() || context.Cars.Any())
            {
                // Database has been seeded
                return;
            }

            // Fill the database with data
            SeedData(context);
        }

        public static void SeedData(DataContext context)
        {
            // Add brands
            var brands = new Brand[]
            {
                new Brand { Name = "Toyota", },
                new Brand { Name = "Honda" },
                new Brand { Name = "Ford" },
                new Brand { Name = "Chevrolet" },
                new Brand { Name = "BMW" },
                new Brand { Name = "Mercedes-Benz" }
            };
            foreach (var brand in brands)
            {
                context.Add(brand);
            }
            context.SaveChanges();
        }
    }
}
