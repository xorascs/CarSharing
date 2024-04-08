using Microsoft.VisualBasic.FileIO;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CarSharing.Models
{
    public class Car
    {
        public int Id { get; set; }

        [Display(Name = "Brand")]
        public int BrandId { get; set; }

        [Display(Name = "Brand")]
        public Brand? Brand { get; set; }

        [Display(Name = "Fuel Type")]
        public FuelTypes FuelType { get; set; }

        [Required]
        [StringLength(50)]
        public required string Model { get; set; }

        public Color Color { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Manufacturing Date"), DataType(DataType.Date)]
        public DateTime ManufacturingDate { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Price ($)/h")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "The Images are required")]
        [Display(Name = "Images")]
        public List<string> ImagePaths { get; set; } = new List<string>();
    }

    public enum Color
    {
        Red,
        Blue,
        Green,
        White,
        Black,
        Silver
    }

    public enum FuelTypes
    {
        Petrol,
        Diesel,
        Electric
    }
}
