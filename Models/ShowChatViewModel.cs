namespace CarSharing.Models
{
    public class ShowChatViewModel
    {
        public Chat? Chat { get; set; }
        public List<User> UsersInChat { get; set; } = new List<User>();
        public List<User> UsersNotInChat { get; set; } = new List<User>();
    }
}
