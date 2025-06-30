using Microsoft.Extensions.Hosting;
using push_notif.Models;
using push_notif.Services;

namespace push_notif.Services
{
    public class NotificationScheduler : BackgroundService
    {
        private readonly List<Meeting> _meetings;
        private readonly PushNotificationService _notifier;

        public NotificationScheduler(List<Meeting> meetings, PushNotificationService notifier)
        {
            _meetings = meetings;
            _notifier = notifier;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("NotificationScheduler started");
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var dueMeetings = _meetings
                    .Where(m => m.MeetingTime > now && m.MeetingTime <= now.AddMinutes(10))
                    .ToList();

                foreach (var meeting in dueMeetings)
                {
                    Console.WriteLine($"Scheduler tick at {DateTime.UtcNow}, meetings: {_meetings.Count}");
                    await _notifier.SendNotificationAsync(meeting);
                    _meetings.Remove(meeting);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}