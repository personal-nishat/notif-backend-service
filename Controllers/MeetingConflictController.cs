using Microsoft.AspNetCore.Mvc;
using push_notif.Models;
using push_notif.Services;

namespace push_notif.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeetingConflictController : ControllerBase
    {
        private readonly PushNotificationService _notifier;

        public MeetingConflictController(PushNotificationService notifier)
        {
            _notifier = notifier;
        }

        [HttpPost]
        public async Task<IActionResult> NotifyMeetingConflict([FromBody] MeetingConflictRequest request)
        {
            var message = $"Meeting {request.Meeting1.MeetingId} at {request.Meeting1.MeetingTime} conflicts with Meeting {request.Meeting2.MeetingId} at {request.Meeting2.MeetingTime}";
            
            await _notifier.SendConflictNotificationAsync(message);
            
            return Ok("Meeting conflict notification sent.");
        }
    }

    // Model for the request
    public class MeetingConflictRequest
    {
        public Meeting Meeting1 { get; set; } = null!;
        public Meeting Meeting2 { get; set; } = null!;
    }
}