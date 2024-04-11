namespace CarSharing.Models
{
    public class BrandsViewModel
    {
        public int Id { get; set; }
        public required string Brand { get; set; }
        public int ElectricCarsCount { get; set; }
    }
}
