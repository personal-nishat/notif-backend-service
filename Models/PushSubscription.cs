namespace push_notif.Models
{
    public class PushSubscription
    {
        public required string Endpoint { get; set; }
        public required SubscriptionKeys Keys { get; set; }
    }

    public class SubscriptionKeys
    {
        public required string P256dh { get; set; }
        public required string Auth { get; set; }
    }
}
