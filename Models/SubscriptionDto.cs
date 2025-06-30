namespace push_notif.Models
{
    public class SubscriptionDto
    {
        public required string Endpoint { get; set; }
        public required KeysDto Keys { get; set; }
    }

    public class KeysDto
    {
        public required string P256dh { get; set; }
        public required string Auth { get; set; }
    }
}