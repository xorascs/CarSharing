namespace CarSharing.Models
{
    public class CarViewModel
    {
        public Car? Car { get; set; }
        public IEnumerable<RatingAndReview> RatingAndReview { get; set; } = new List<RatingAndReview>();
    }
}
