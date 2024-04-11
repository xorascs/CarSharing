namespace CarSharing.Models
{
        public class UserViewModel
        {
            public User? User { get; set; }
            public IEnumerable<Rents> Rents { get; set; } = new List<Rents>();
        }
}
