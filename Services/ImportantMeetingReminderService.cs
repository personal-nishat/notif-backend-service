using Microsoft.Extensions.Hosting;
using push_notif.Models;
using push_notif.Services;
using push_notif.Controllers;

namespace push_notif.Services
{
    public class ImportantMeetingReminderService : BackgroundService
    {
        private readonly PushNotificationService _notifier;
        private readonly HashSet<string> _sentReminders = new();

        public ImportantMeetingReminderService(PushNotificationService notifier)
        {
            _notifier = notifier;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("ImportantMeetingReminderService started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var twoHoursFromNow = now.AddHours(2);
                    
                    Console.WriteLine($"Checking for important meetings. Current time: {now}, Looking for meetings around: {twoHoursFromNow}");
                    
                    var storedMeetings = MeetingController.GetAllStoredMeetings();
                    Console.WriteLine($"Total stored meetings: {storedMeetings.Count}");
                    
                    foreach (var meeting in storedMeetings)
                    {
                        Console.WriteLine($"Checking meeting {meeting.MeetingId} starting at {meeting.MeetingStartTime}");
                        Console.WriteLine($"Meeting attendees: {string.Join(", ", meeting.Attendees)}");
                        
                        // Check if meeting starts in approximately 2 hours (within 120-minute window for testing)
                        var timeDifference = Math.Abs((meeting.MeetingStartTime - twoHoursFromNow).TotalMinutes);
                        Console.WriteLine($"Time difference: {timeDifference} minutes");
                        
                        if (timeDifference <= 120 && !_sentReminders.Contains(meeting.MeetingId))
                        {
                            Console.WriteLine($"Meeting {meeting.MeetingId} is within timing window");
                            
                            // Check if this is an important meeting
                            bool isImportant = ImportantPeopleController.IsImportantMeeting(meeting.Attendees);
                            Console.WriteLine($"Is meeting {meeting.MeetingId} important? {isImportant}");
                            
                            if (isImportant)
                            {
                                Console.WriteLine($"Sending 2-hour reminder for important meeting: {meeting.MeetingId}");
                                await _notifier.SendImportantMeetingReminderAsync(meeting.MeetingId);
                                _sentReminders.Add(meeting.MeetingId);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Meeting {meeting.MeetingId} not in timing window or already sent");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ImportantMeetingReminderService: {ex}");
                }

                // Check every minute for testing
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}