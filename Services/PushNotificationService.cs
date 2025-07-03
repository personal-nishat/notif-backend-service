using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using push_notif.Models;
using push_notif.Controllers;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace push_notif.Services
{
    public class PushNotificationService
    {
        private readonly PushServiceClient _client;
        private static readonly Dictionary<string, Timer> _scheduledNotifications = new();

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

        private DateTime ConvertToIST(DateTime utcTime)
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, istTimeZone);
        }

        public async Task SendNotificationAsync(Meeting meeting)
        {
            Console.WriteLine($"Total subscriptions: {SubscriptionController.GetAll().Count}");
            try
            {
                // Convert meeting time to IST for display
                var meetingTimeIST = ConvertToIST(meeting.MeetingStartTime);
                var payload = $"{{\"title\":\"Meeting Reminder\",\"body\":\"Meeting {meeting.MeetingId} starts at {meetingTimeIST:dd/MM/yyyy hh:mm tt} IST\"}}";

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

        public async Task SendBreakNotificationAsync(string message)
        {
            var payload = $"{{\"title\":\"Break Reminder\",\"body\":\"{message}\",\"icon\":\"/break-icon.png\"}}";
            
            Console.WriteLine($"Sending break notification: {message}");
            
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
                    Console.WriteLine("Break notification sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Break notification failed: {ex}");
                }
            }
        }

        public async Task SendImmediateMeetingReminderAsync(Meeting meeting)
        {
            var meetingTimeIST = ConvertToIST(meeting.MeetingStartTime);
            var meetingEndIST = ConvertToIST(meeting.MeetingEndTime);
            var now = DateTime.UtcNow;
            var minutesUntilMeeting = (meeting.MeetingStartTime - now).TotalMinutes;

            string timeMessage;
            if (minutesUntilMeeting <= 0)
            {
                timeMessage = "starting now!";
            }
            else if (minutesUntilMeeting <= 1)
            {
                timeMessage = "starting in less than 1 minute!";
            }
            else if (minutesUntilMeeting <= 10)
            {
                timeMessage = $"starting in {Math.Round(minutesUntilMeeting)} minutes!";
            }
            else
            {
                timeMessage = $"starts at {meetingTimeIST:HH:mm} IST";
            }

            var payload = $"{{\"title\":\"Meeting Selected - {meeting.MeetingId}\",\"body\":\"Your meeting {timeMessage} Time: {meetingTimeIST:dd/MM/yyyy HH:mm} - {meetingEndIST:HH:mm} IST. Attendees: {string.Join(", ", meeting.Attendees)}\"}}";

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
                    Console.WriteLine($"Immediate meeting reminder sent for {meeting.MeetingId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Push message failed: {ex}");
                }
            }
        }

        public void ScheduleTenMinuteReminderAsync(Meeting meeting)
        {
            var now = DateTime.UtcNow;
            var tenMinutesBeforeMeeting = meeting.MeetingStartTime.AddMinutes(-10);
            var delayUntilReminder = tenMinutesBeforeMeeting - now;

            if (delayUntilReminder.TotalMilliseconds <= 0)
            {
                // If the 10-minute mark has already passed, send immediate reminder
                Console.WriteLine($"10-minute mark already passed for {meeting.MeetingId}, sending immediate reminder");
                _ = Task.Run(async () => await SendImmediateMeetingReminderAsync(meeting));
                return;
            }

            Console.WriteLine($"Scheduling 10-minute reminder for {meeting.MeetingId} in {delayUntilReminder.TotalMinutes:F1} minutes");

            // Cancel any existing timer for this meeting
            if (_scheduledNotifications.ContainsKey(meeting.MeetingId))
            {
                _scheduledNotifications[meeting.MeetingId].Dispose();
            }

            // Schedule the reminder
            var timer = new Timer(async _ =>
            {
                Console.WriteLine($"Sending scheduled 10-minute reminder for {meeting.MeetingId}");
                await SendImmediateMeetingReminderAsync(meeting);
                
                // Clean up the timer
                if (_scheduledNotifications.ContainsKey(meeting.MeetingId))
                {
                    _scheduledNotifications[meeting.MeetingId].Dispose();
                    _scheduledNotifications.Remove(meeting.MeetingId);
                }
            }, null, delayUntilReminder, TimeSpan.FromMilliseconds(-1)); // Fire once only

            _scheduledNotifications[meeting.MeetingId] = timer;
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

        public async Task SendImportantMeetingReminderAsync(string meetingId)
        {
            var message = $"Important meeting {meetingId} is about to start in 2 hours. Do you want to prepare for this?";
            var payload = $"{{\"title\":\"Important Meeting Reminder\",\"body\":\"{message}\"}}";

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

        public async Task SendInteractiveConflictNotificationAsync(string conflictId, Meeting meeting1, Meeting meeting2)
        {
            var meeting1StartIST = ConvertToIST(meeting1.MeetingStartTime);
            var meeting1EndIST = ConvertToIST(meeting1.MeetingEndTime);
            var meeting2StartIST = ConvertToIST(meeting2.MeetingStartTime);
            var meeting2EndIST = ConvertToIST(meeting2.MeetingEndTime);

            // Use proper JSON serialization
            var notificationData = new
            {
                title = "Meeting Conflict Detected!",
                body = "Please choose one of the following meetings:",
                icon = "/icon.png",
                badge = "/badge.png",
                tag = conflictId,
                requireInteraction = true,
                data = new
                {
                    conflictId = conflictId,
                    meeting1 = new
                    {
                        id = meeting1.MeetingId,
                        time = $"{meeting1StartIST:dd/MM/yyyy HH:mm} - {meeting1EndIST:HH:mm} IST",
                        attendees = string.Join(", ", meeting1.Attendees)
                    },
                    meeting2 = new
                    {
                        id = meeting2.MeetingId,
                        time = $"{meeting2StartIST:dd/MM/yyyy HH:mm} - {meeting2EndIST:HH:mm} IST",
                        attendees = string.Join(", ", meeting2.Attendees)
                    }
                },
                actions = new[]
                {
                    new
                    {
                        action = $"select_{meeting1.MeetingId}",
                        title = $"{meeting1.MeetingId} ({meeting1StartIST:HH:mm}-{meeting1EndIST:HH:mm})",
                        icon = "/select-icon.png"
                    },
                    new
                    {
                        action = $"select_{meeting2.MeetingId}",
                        title = $"{meeting2.MeetingId} ({meeting2StartIST:HH:mm}-{meeting2EndIST:HH:mm})",
                        icon = "/select-icon.png"
                    }
                }
            };

            var payload = JsonSerializer.Serialize(notificationData);
            Console.WriteLine($"Sending interactive notification payload: {payload}");

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
                    Console.WriteLine($"Interactive conflict notification sent for {conflictId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Push message failed: {ex}");
                }
            }
        }
    }
}