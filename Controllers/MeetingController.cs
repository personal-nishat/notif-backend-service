using Microsoft.AspNetCore.Mvc;
using push_notif.Models;

namespace push_notif.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeetingController : ControllerBase
    {
        private readonly List<Meeting> _meetings;

        public MeetingController(List<Meeting> meetings)
        {
            _meetings = meetings;
        }

        [HttpPost]
        public IActionResult ScheduleMeeting([FromBody] Meeting meeting)
        {
            _meetings.Add(meeting);
            return Ok($"Meeting {meeting.MeetingId} scheduled at {meeting.MeetingTime}");
        }
    }
}
