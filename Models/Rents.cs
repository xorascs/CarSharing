using System.ComponentModel.DataAnnotations;

namespace CarSharing.Models
{
    public class Rents
    {
        public int Id { get; set; }
        [Display(Name = "User")]
        public int UserId { get; set; }
        public User? User { get; set; }
        [Display(Name = "Car")]
        public int CarId { get; set; }
        public Car? Car { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Rent Time"), DataType(DataType.Date)]
        public DateTime RentTime { get; set; }
        [Display(Name = "Time for Rent")]
        public int TimeForRent { get; set; }
        [Required]
        [DataType(DataType.CreditCard)]
        [Display(Name = "Card Number")]
        [MaxLength(16)]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "The card number must contain exactly 16 numeric characters.")]
        public required string CardNumber { get; set; }
        [Required]
        [Display(Name = "Card Holder Name")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only alphabetic characters are allowed.")]
        public required string CardHolderName { get; set; }
        [Required]
        [DisplayFormat(DataFormatString = "{0:MM/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Card Expiration Date")]
        public DateTime CardExDate { get; set;}
        [Range(100, 999)]
        public int CVV {  get; set; }
    }
}
