namespace CarSharing.Models
{
        public class UserViewModel
        {
            public User? User { get; set; }
            public IEnumerable<Rents> Rents { get; set; } = new List<Rents>();
            public IEnumerable<RatingAndReview> Reviews { get; set; } = new List<RatingAndReview>();

            public int totalRents { get; set; }
            public decimal totalDollarsForCars { get; set; }
    }
}
