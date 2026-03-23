namespace PontelloApp.Models
{
    public class ScheduledEmail
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public byte[] AttachmentBytes { get; set; }
        public string AttachmentName { get; set; }
        public DateTime NextSendAt { get; set; }
        public TimeSpan RepeatInterval { get; set; }

        public bool IsActive { get; set; }

    }
}
