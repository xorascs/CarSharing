namespace CarSharing.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public List<User> UsersList { get; set; } = new List<User>();
        public List<Message> Messages {  get; set; } = new List<Message>();
    }
}
