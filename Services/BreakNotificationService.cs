using push_notif.Models;
using push_notif.Controllers;

namespace push_notif.Services
{
    public class BreakNotificationService
    {
        private readonly PushNotificationService _notifier;
        private static readonly Dictionary<string, Timer> _scheduledBreakNotifications = new();

        public BreakNotificationService(PushNotificationService notifier)
        {
            _notifier = notifier;
        }

        public void AnalyzeAndScheduleBreakNotifications(List<Meeting> meetings)
        {
            Console.WriteLine($"Analyzing {meetings.Count} meetings for break notifications");
            
            // Sort meetings by start time
            var sortedMeetings = meetings.OrderBy(m => m.MeetingStartTime).ToList();
            
            // Find sequences of back-to-back meetings
            var meetingSequences = FindBackToBackSequences(sortedMeetings);
            
            foreach (var sequence in meetingSequences)
            {
                var totalDuration = (sequence.Last().MeetingEndTime - sequence.First().MeetingStartTime).TotalHours;
                
                if (totalDuration >= 2.0)
                {
                    Console.WriteLine($"Found meeting sequence of {totalDuration:F1} hours: {string.Join(", ", sequence.Select(m => m.MeetingId))}");
                    ScheduleBreakNotification(sequence);
                }
            }
        }

        private List<List<Meeting>> FindBackToBackSequences(List<Meeting> sortedMeetings)
        {
            var sequences = new List<List<Meeting>>();
            var currentSequence = new List<Meeting>();

            for (int i = 0; i < sortedMeetings.Count; i++)
            {
                var currentMeeting = sortedMeetings[i];
                
                if (currentSequence.Count == 0)
                {
                    // Start a new sequence
                    currentSequence.Add(currentMeeting);
                }
                else
                {
                    var lastMeeting = currentSequence.Last();
                    var timeBetween = (currentMeeting.MeetingStartTime - lastMeeting.MeetingEndTime).TotalMinutes;
                    
                    if (timeBetween <= 15) // Consider meetings back-to-back if within 15 minutes
                    {
                        // Extend current sequence
                        currentSequence.Add(currentMeeting);
                    }
                    else
                    {
                        // End current sequence and start a new one
                        if (currentSequence.Count > 0)
                        {
                            sequences.Add(new List<Meeting>(currentSequence));
                        }
                        currentSequence.Clear();
                        currentSequence.Add(currentMeeting);
                    }
                }
            }
            
            // Add the last sequence if it exists
            if (currentSequence.Count > 0)
            {
                sequences.Add(currentSequence);
            }
            
            return sequences;
        }

        private void ScheduleBreakNotification(List<Meeting> meetingSequence)
        {
            var lastMeeting = meetingSequence.Last();
            var sequenceId = string.Join("-", meetingSequence.Select(m => m.MeetingId));
            var now = DateTime.UtcNow;
            var delayUntilSequenceEnd = lastMeeting.MeetingEndTime - now;
            
            if (delayUntilSequenceEnd.TotalMilliseconds <= 0)
            {
                // Sequence has already ended, send immediate notification
                Console.WriteLine($"Meeting sequence {sequenceId} has already ended, sending immediate break notification");
                _ = Task.Run(async () => await SendBreakNotificationAsync(meetingSequence));
                return;
            }

            Console.WriteLine($"Scheduling break notification for sequence {sequenceId} in {delayUntilSequenceEnd.TotalMinutes:F1} minutes");

            // Cancel any existing timer for this sequence
            if (_scheduledBreakNotifications.ContainsKey(sequenceId))
            {
                _scheduledBreakNotifications[sequenceId].Dispose();
            }

            // Schedule the break notification
            var timer = new Timer(async _ =>
            {
                Console.WriteLine($"Sending break notification for sequence {sequenceId}");
                await SendBreakNotificationAsync(meetingSequence);
                
                // Clean up
                if (_scheduledBreakNotifications.ContainsKey(sequenceId))
                {
                    _scheduledBreakNotifications[sequenceId].Dispose();
                    _scheduledBreakNotifications.Remove(sequenceId);
                }
                
            }, null, delayUntilSequenceEnd, TimeSpan.FromMilliseconds(-1)); // Fire once only

            _scheduledBreakNotifications[sequenceId] = timer;
        }

        private async Task SendBreakNotificationAsync(List<Meeting> meetingSequence)
        {
            var firstMeeting = meetingSequence.First();
            var lastMeeting = meetingSequence.Last();
            var totalDuration = (lastMeeting.MeetingEndTime - firstMeeting.MeetingStartTime).TotalHours;
            var sequenceEndTimeIST = ConvertToIST(lastMeeting.MeetingEndTime);
            
            var meetingIds = string.Join(", ", meetingSequence.Select(m => m.MeetingId));
            var message = $"You have attended meetings ({meetingIds}) for {totalDuration:F1} hours (until {sequenceEndTimeIST:HH:mm} IST). Do you want to take a break?";
            
            await _notifier.SendBreakNotificationAsync(message);
        }

        private DateTime ConvertToIST(DateTime utcTime)
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, istTimeZone);
        }

        public static void ClearAllScheduledBreakNotifications()
        {
            // Cancel all scheduled notifications
            foreach (var timer in _scheduledBreakNotifications.Values)
            {
                timer.Dispose();
            }
            _scheduledBreakNotifications.Clear();
            
            Console.WriteLine("All scheduled break notifications cleared");
        }
    }
}