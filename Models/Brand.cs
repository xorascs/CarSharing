using System.ComponentModel.DataAnnotations;

namespace CarSharing.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        public ICollection<Car> Cars { get; set; } = new List<Car>();
    }
}
