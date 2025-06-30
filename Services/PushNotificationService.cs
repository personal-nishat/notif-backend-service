using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using push_notif.Models;
using push_notif.Controllers;
using Microsoft.Extensions.Configuration;

namespace push_notif.Services
{
    public class PushNotificationService
    {
        private readonly PushServiceClient _client;

        public PushNotificationService(IConfiguration config)
        {
            _client = new PushServiceClient
            {
                DefaultAuthentication = new VapidAuthentication(
                    config["VapidDetails:PublicKey"],
                    config["VapidDetails:PrivateKey"]
                )
            };
        }

        public async Task SendNotificationAsync(Meeting meeting)
        {
            Console.WriteLine($"Total subscriptions: {SubscriptionController.GetAll().Count}");
            try
            {
                var payload = $"{{\"title\":\"Meeting Reminder\",\"body\":\"Meeting {meeting.MeetingId} starts at {meeting.MeetingTime}\"}}";
                foreach (var sub in SubscriptionController.GetAll())
                {
                    Console.WriteLine($"Sending notification to: {sub.Endpoint} for meeting {meeting.MeetingId}");
                    var subscription = new Lib.Net.Http.WebPush.PushSubscription
                    {
                        Endpoint = sub.Endpoint,
                        Keys = new Dictionary<string, string>
                    {
                        { "p256dh", sub.Keys.P256dh },
                        { "auth", sub.Keys.Auth }
                    }
                    };
                    Console.WriteLine($"Total subscriptions: {SubscriptionController.GetAll().Count}");
                    Console.WriteLine("About to send push message...");
                    var message = new PushMessage(payload);

                    try
                    {
                        await _client.RequestPushMessageDeliveryAsync(subscription, message);
                        Console.WriteLine("Push message sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Push message failed: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification error: {ex}");
            }

        }
        public async Task SendCustomNotificationAsync(string message)
        {
            var payload = $"{{\"title\":\"RSVP Reminder\",\"body\":\"{message}\"}}";
            foreach (var sub in Controllers.SubscriptionController.GetAll())
            {
                var subscription = new Lib.Net.Http.WebPush.PushSubscription
                {
                    Endpoint = sub.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        { "p256dh", sub.Keys.P256dh },
                        { "auth", sub.Keys.Auth }
                    }
                };
                var pushMessage = new PushMessage(payload);
                try
                {
                    await _client.RequestPushMessageDeliveryAsync(subscription, pushMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Push message failed: {ex}");
                }
            }
        }
        public async Task SendConflictNotificationAsync(string message)
        {
            var payload = $"{{\"title\":\"Meeting Conflict Alert\",\"body\":\"{message}\"}}";
            foreach (var sub in Controllers.SubscriptionController.GetAll())
            {
                var subscription = new Lib.Net.Http.WebPush.PushSubscription
                {
                    Endpoint = sub.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        { "p256dh", sub.Keys.P256dh },
                        { "auth", sub.Keys.Auth }
                    }
                };
                var pushMessage = new PushMessage(payload);
                try
                {
                    await _client.RequestPushMessageDeliveryAsync(subscription, pushMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Push message failed: {ex}");
                }
            }
        }

    }
}
