using Microsoft.AspNetCore.Mvc;
using push_notif.Models;
using push_notif.Services;

namespace push_notif.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NoneResponseController : ControllerBase
    {
        private readonly PushNotificationService _notifier;

        public NoneResponseController(PushNotificationService notifier)
        {
            _notifier = notifier;
        }

        private DateTime ConvertToIST(DateTime utcTime)
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, istTimeZone);
        }

        [HttpPost]
        public async Task<IActionResult> NoneResponse([FromBody] Meeting meeting)
        {
            // Convert meeting time to IST for display
            var meetingTimeIST = ConvertToIST(meeting.MeetingStartTime);
            var message = $"You haven't RSVPed for {meeting.MeetingId} which starts at {meetingTimeIST:dd/MM/yyyy hh:mm tt} IST";
            
            await _notifier.SendCustomNotificationAsync(message);
            return Ok("NoneResponse notification sent.");
        }
    }
}