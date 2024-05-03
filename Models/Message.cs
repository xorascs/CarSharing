namespace CarSharing.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ChatId { get; set; }
        public required string Text { get; set; }
        public DateTime CreateTime { get; set; }

        public User? User { get; set; }
        public Chat? Chat { get; set; }
    }
}
