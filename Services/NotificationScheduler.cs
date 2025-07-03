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
                    var tenMinutesFromNow = now.AddMinutes(10);
                    
                    var storedMeetings = MeetingController.GetAllStoredMeetings().ToList();
                    
                    foreach (var meeting in storedMeetings)
                    {
                        // Check if this meeting is suppressed by user choice
                        if (MeetingChoiceController.IsMeetingSuppressed(meeting.MeetingId))
                        {
                            Console.WriteLine($"Meeting {meeting.MeetingId} is suppressed by user choice. Skipping notification.");
                            continue;
                        }

                        var timeDifference = Math.Abs((meeting.MeetingStartTime - tenMinutesFromNow).TotalMinutes);
                        
                        if (timeDifference <= 1 && !_sentReminders.Contains(meeting.MeetingId))
                        {
                            Console.WriteLine($"Sending 10-minute reminder for meeting: {meeting.MeetingId}");
                            await _notifier.SendNotificationAsync(meeting);
                            _sentReminders.Add(meeting.MeetingId);
                        }
                        
                        if (meeting.MeetingStartTime <= now && _sentReminders.Contains(meeting.MeetingId))
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