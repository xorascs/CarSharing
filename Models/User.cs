using System.ComponentModel.DataAnnotations;

namespace CarSharing.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required, Display(Name = "Role")]
        public bool IsAdmin { get; set; }
        [Required]
        public required string Login {  get; set; }
        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
        [Required]
        public required string Name { get; set; }
        [Required]
        public required string Surname { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }
        [Required]
        public required string Address {  get; set; }
        [Required]
        [Display(Name = "Postal Code")]
        [DataType(DataType.PostalCode)]
        public required string PostalCode { get; set; }

        public List<Chat> ChatsList { get; set; } = new List<Chat>();
    }
}
