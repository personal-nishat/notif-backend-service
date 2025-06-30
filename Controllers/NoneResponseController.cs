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

        [HttpPost]
        public async Task<IActionResult> NoneResponse([FromBody] Meeting meeting)
        {
            var message = $"You haven't RSVPed for {meeting.MeetingId} which starts at {meeting.MeetingTime}";
            await _notifier.SendCustomNotificationAsync(message);
            return Ok("NoneResponse notification sent.");
        }
    }
}