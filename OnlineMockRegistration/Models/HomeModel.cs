namespace OnlineMockRegistration.Models
{
    public class PostNameEmailInputModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class MessageModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTimeOffset CreatedTimestamp { get; set; }
    }
}
