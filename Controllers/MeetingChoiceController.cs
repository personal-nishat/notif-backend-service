using Microsoft.AspNetCore.Mvc;
using push_notif.Models;
using push_notif.Services;

namespace push_notif.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeetingChoiceController : ControllerBase
    {
        private readonly PushNotificationService _notifier;
        private static readonly Dictionary<string, MeetingChoice> _userChoices = new();
        private static readonly HashSet<string> _suppressedMeetings = new();

        public MeetingChoiceController(PushNotificationService notifier)
        {
            _notifier = notifier;
        }

        [HttpPost]
        public async Task<IActionResult> ChooseMeeting([FromBody] MeetingChoiceRequest request)
        {
            var choice = new MeetingChoice
            {
                ConflictId = request.ConflictId,
                SelectedMeetingId = request.SelectedMeetingId,
                RejectedMeetingId = GetRejectedMeetingId(request.ConflictId, request.SelectedMeetingId)
            };

            _userChoices[request.ConflictId] = choice;
            _suppressedMeetings.Add(choice.RejectedMeetingId);

            // Send confirmation notification
            var message = $"Choice confirmed! You will attend {choice.SelectedMeetingId}.";
            await _notifier.SendCustomNotificationAsync(message);

            // Handle reminder for the selected meeting
            var selectedMeeting = MeetingController.GetAllStoredMeetings()
                .FirstOrDefault(m => m.MeetingId == choice.SelectedMeetingId);
            
            if (selectedMeeting != null)
            {
                var now = DateTime.UtcNow;
                var minutesUntilMeeting = (selectedMeeting.MeetingStartTime - now).TotalMinutes;

                if (minutesUntilMeeting <= 10)
                {
                    // Send immediate reminder if 10 minutes or less
                    Console.WriteLine($"Sending immediate reminder for selected meeting: {choice.SelectedMeetingId} (starts in {minutesUntilMeeting:F1} minutes)");
                    await _notifier.SendImmediateMeetingReminderAsync(selectedMeeting);
                }
                else
                {
                    // Schedule reminder for 10 minutes before the meeting
                    Console.WriteLine($"Scheduling 10-minute reminder for selected meeting: {choice.SelectedMeetingId} (starts in {minutesUntilMeeting:F1} minutes)");
                    _notifier.ScheduleTenMinuteReminderAsync(selectedMeeting);
                }
            }

            Console.WriteLine($"User selected {choice.SelectedMeetingId} over {choice.RejectedMeetingId} for conflict {request.ConflictId}");

            return Ok(new 
            {
                Message = "Meeting choice recorded successfully",
                SelectedMeeting = choice.SelectedMeetingId,
                SuppressedMeeting = choice.RejectedMeetingId
            });
        }

        [HttpGet("choices")]
        public IActionResult GetUserChoices()
        {
            return Ok(new { Choices = _userChoices.Values, SuppressedMeetings = _suppressedMeetings });
        }

        [HttpDelete("choices")]
        public IActionResult ClearChoices()
        {
            _userChoices.Clear();
            _suppressedMeetings.Clear();
            return Ok("All choices cleared");
        }

        public static bool IsMeetingSuppressed(string meetingId)
        {
            return _suppressedMeetings.Contains(meetingId);
        }

        private static string GetRejectedMeetingId(string conflictId, string selectedMeetingId)
        {
            // Extract the other meeting ID from the conflict ID
            // This assumes conflict ID format: "MEET001_vs_MEET002"
            var parts = conflictId.Split("_vs_");
            return parts[0] == selectedMeetingId ? parts[1] : parts[0];
        }
    }
}