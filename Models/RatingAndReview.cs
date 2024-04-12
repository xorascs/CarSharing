using System.ComponentModel.DataAnnotations;

namespace CarSharing.Models
{
    public class RatingAndReview
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int CarId { get; set; }
        [Required(ErrorMessage = "Comment is required.")]
        public required string Comment { get; set; }
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }
        [Required]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Created at"), DataType(DataType.Date)]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Car? Car { get; set; }
    }

}
