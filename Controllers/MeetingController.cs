using Microsoft.AspNetCore.Mvc;
using push_notif.Models;
using push_notif.Services;

namespace push_notif.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeetingController : ControllerBase
    {
        private readonly List<Meeting> _meetings;
        private readonly PushNotificationService _notifier;
        private readonly BreakNotificationService _breakNotificationService;
        private static readonly HashSet<Meeting> _storedMeetings = [];
        private static readonly Dictionary<string, string> _sentConflicts = [];
        private static readonly object _conflictLock = new();

        public MeetingController(List<Meeting> meetings, PushNotificationService notifier, BreakNotificationService breakNotificationService)
        {
            _meetings = meetings;
            _notifier = notifier;
            _breakNotificationService = breakNotificationService;
        }

        private static DateTime ConvertToIST(DateTime utcTime)
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, istTimeZone);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessMeetings([FromBody] MeetingBatchRequest request)
        {
            var conflicts = new List<string>();
            var newMeetings = new List<Meeting>();
            var duplicateMeetings = new List<string>();
            
            Console.WriteLine($"Processing {request.Meetings.Count} meetings");
            
            // Process each meeting
            foreach (var meeting in request.Meetings)
            {
                Console.WriteLine($"Meeting: {meeting.MeetingId} from {meeting.MeetingStartTime} to {meeting.MeetingEndTime}");
                
                // Check if meeting already exists (by MeetingId)
                if (_storedMeetings.Any(m => m.MeetingId == meeting.MeetingId))
                {
                    duplicateMeetings.Add(meeting.MeetingId);
                    continue;
                }

                // Validate attendees count (1-5)
                if (meeting.Attendees.Count < 1 || meeting.Attendees.Count > 5)
                {
                    return BadRequest($"Meeting {meeting.MeetingId} must have 1-5 attendees. Current count: {meeting.Attendees.Count}");
                }

                newMeetings.Add(meeting);
            }

            Console.WriteLine($"Added {newMeetings.Count} new meetings to process");

            // Check for conflicts between new meetings and existing ones
            foreach (var newMeeting in newMeetings)
            {
                // Check against existing stored meetings
                foreach (var existingMeeting in _storedMeetings)
                {
                    Console.WriteLine($"Checking new meeting {newMeeting.MeetingId} vs existing {existingMeeting.MeetingId}");
                    
                    if (DoMeetingsConflict(newMeeting, existingMeeting))
                    {
                        var conflictId = GenerateConflictId(newMeeting.MeetingId, existingMeeting.MeetingId);
                        
                        string? conflictMessage = null; // Make nullable explicit
                        bool shouldSendNotification = false;
                        
                        lock (_conflictLock)
                        {
                            if (!_sentConflicts.ContainsKey(conflictId))
                            {
                                conflictMessage = CreateConflictMessage(newMeeting, existingMeeting, conflictId);
                                conflicts.Add(conflictMessage);
                                _sentConflicts[conflictId] = conflictMessage;
                                shouldSendNotification = true;
                            }
                        }
                        
                        // Send notification outside the lock - add null check
                        if (shouldSendNotification && conflictMessage != null)
                        {
                            await _notifier.SendInteractiveConflictNotificationAsync(conflictId, newMeeting, existingMeeting);
                        }
                    }
                }
            }

            // Check for conflicts between new meetings in this batch
            Console.WriteLine($"Checking conflicts between {newMeetings.Count} new meetings in batch");
            for (int i = 0; i < newMeetings.Count; i++)
            {
                for (int j = i + 1; j < newMeetings.Count; j++)
                {
                    var meeting1 = newMeetings[i];
                    var meeting2 = newMeetings[j];
                    
                    Console.WriteLine($"Comparing {meeting1.MeetingId} vs {meeting2.MeetingId}");
                    
                    if (DoMeetingsConflict(meeting1, meeting2))
                    {
                        var conflictId = GenerateConflictId(meeting1.MeetingId, meeting2.MeetingId);
                        
                        string? conflictMessage = null; // Explicitly nullable
                        bool shouldSendNotification = false;
                        
                        lock (_conflictLock)
                        {
                            if (!_sentConflicts.ContainsKey(conflictId))
                            {
                                conflictMessage = CreateConflictMessage(meeting1, meeting2, conflictId);
                                conflicts.Add(conflictMessage);
                                _sentConflicts[conflictId] = conflictMessage;
                                shouldSendNotification = true;
                            }
                        }
                        
                        // Send notification outside the lock to avoid await in lock
                        if (shouldSendNotification && conflictMessage != null)
                        {
                            Console.WriteLine($"Sending interactive notification for conflict {conflictId}");
                            await _notifier.SendInteractiveConflictNotificationAsync(conflictId, meeting1, meeting2);
                        }
                    }
                }
            }

            // Add new meetings to storage
            foreach (var meeting in newMeetings)
            {
                _storedMeetings.Add(meeting);
                _meetings.Add(meeting);
            }

            // Check storage limit after adding meetings
            if (_storedMeetings.Count > 25)
            {
                return BadRequest($"Cannot store more than 25 meetings. Current count: {_storedMeetings.Count}");
            }

            // Analyze all stored meetings for break notifications
            Console.WriteLine("Analyzing meetings for break notifications");
            _breakNotificationService.AnalyzeAndScheduleBreakNotifications(_storedMeetings.ToList());

            Console.WriteLine($"Final result: {conflicts.Count} conflicts found");

            return Ok(new
            {
                ProcessedMeetings = newMeetings.Count,
                ConflictsFound = conflicts.Count,
                Conflicts = conflicts,
                TotalStoredMeetings = _storedMeetings.Count,
                Message = $"Processed {newMeetings.Count} new meetings. Found {conflicts.Count} conflicts. Analyzed for break notifications."
            });
        }

        [HttpGet("stored")]
        public IActionResult GetStoredMeetings()
        {
            return Ok(new { TotalMeetings = _storedMeetings.Count, Meetings = _storedMeetings });
        }

        [HttpDelete("clear")]
        public IActionResult ClearStoredMeetings()
        {
            _storedMeetings.Clear();
            _meetings.Clear();
            _sentConflicts.Clear(); // Also clear sent conflicts
            BreakNotificationService.ClearAllScheduledBreakNotifications();
            return Ok("All stored meetings and scheduled break notifications cleared.");
        }

        public static List<Meeting> GetAllStoredMeetings()
        {
            return [.. _storedMeetings];
        }

        public static void RemoveMeeting(string meetingId)
        {
            _storedMeetings.RemoveWhere(m => m.MeetingId == meetingId);
        }

        private static string GenerateConflictId(string meetingId1, string meetingId2)
        {
            // Always order the IDs consistently to avoid duplicate conflicts
            var orderedIds = new[] { meetingId1, meetingId2 }.OrderBy(x => x).ToArray();
            return $"{orderedIds[0]}_vs_{orderedIds[1]}";
        }

        private static string CreateConflictMessage(Meeting meeting1, Meeting meeting2, string conflictId)
        {
            var meeting1StartIST = ConvertToIST(meeting1.MeetingStartTime);
            var meeting1EndIST = ConvertToIST(meeting1.MeetingEndTime);
            var meeting2StartIST = ConvertToIST(meeting2.MeetingStartTime);
            var meeting2EndIST = ConvertToIST(meeting2.MeetingEndTime);
            
            return $"CONFLICT {conflictId}: Meeting {meeting1.MeetingId} ({meeting1StartIST:dd/MM/yyyy HH:mm} - {meeting1EndIST:HH:mm} IST) conflicts with Meeting {meeting2.MeetingId} ({meeting2StartIST:dd/MM/yyyy HH:mm} - {meeting2EndIST:HH:mm} IST). Choose which meeting to attend.";
        }

        private static bool DoMeetingsConflict(Meeting meeting1, Meeting meeting2)
        {
            return meeting1.MeetingStartTime < meeting2.MeetingEndTime && 
                   meeting2.MeetingStartTime < meeting1.MeetingEndTime;
        }
    }

    public class MeetingBatchRequest
    {
        public required List<Meeting> Meetings { get; set; }
    }
}