using Microsoft.Extensions.Hosting;
using push_notif.Models;
using push_notif.Services;
using push_notif.Controllers;

namespace push_notif.Services
{
    public class NotificationScheduler : BackgroundService
    {
        private readonly List<Meeting> _meetings;
        private readonly PushNotificationService _notifier;
        private readonly HashSet<string> _sentReminders = new();

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
                try
                {
                    var now = DateTime.UtcNow;
                    var storedMeetings = MeetingController.GetAllStoredMeetings().ToList();
                    
                    Console.WriteLine($"NotificationScheduler checking {storedMeetings.Count} meetings at {now:HH:mm:ss}");
                    
                    foreach (var meeting in storedMeetings)
                    {
                        // Check if this meeting is suppressed by user choice
                        if (MeetingChoiceController.IsMeetingSuppressed(meeting.MeetingId))
                        {
                            Console.WriteLine($"Meeting {meeting.MeetingId} is suppressed by user choice. Skipping notification.");
                            continue;
                        }

                        var minutesUntilMeeting = (meeting.MeetingStartTime - now).TotalMinutes;
                        
                        // Send reminder if meeting is 10 minutes or less away and haven't sent reminder yet
                        if (minutesUntilMeeting <= 10 && minutesUntilMeeting > 0 && !_sentReminders.Contains(meeting.MeetingId))
                        {
                            Console.WriteLine($"Sending reminder for meeting: {meeting.MeetingId} (starts in {minutesUntilMeeting:F1} minutes)");
                            await _notifier.SendNotificationAsync(meeting);
                            _sentReminders.Add(meeting.MeetingId);
                        }
                        
                        // Remove meeting from system after it has started
                        if (meeting.MeetingStartTime <= now)
                        {
                            Console.WriteLine($"Meeting {meeting.MeetingId} has started. Removing from system...");
                            MeetingController.RemoveMeeting(meeting.MeetingId);
                            _sentReminders.Remove(meeting.MeetingId);
                            
                            var meetingInList = _meetings.FirstOrDefault(m => m.MeetingId == meeting.MeetingId);
                            if (meetingInList != null)
                            {
                                _meetings.Remove(meetingInList);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in NotificationScheduler: {ex}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
