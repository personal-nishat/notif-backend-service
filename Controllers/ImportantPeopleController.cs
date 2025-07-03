using Microsoft.AspNetCore.Mvc;
using push_notif.Services;

namespace push_notif.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImportantPeopleController : ControllerBase
    {
        private readonly PushNotificationService _notifier;
        private static readonly HashSet<string> _importantPeople = new();

        public ImportantPeopleController(PushNotificationService notifier)
        {
            _notifier = notifier;
        }

        [HttpPost]
        public IActionResult SetImportantPeople([FromBody] ImportantPeopleRequest request)
        {
            // Validate count (1-5 people)
            if (request.ImportantPeople.Count < 1 || request.ImportantPeople.Count > 5)
            {
                return BadRequest($"Must have 1-5 important people. Current count: {request.ImportantPeople.Count}");
            }

            // Clear existing and add new important people
            _importantPeople.Clear();
            foreach (var person in request.ImportantPeople)
            {
                _importantPeople.Add(person);
            }

            return Ok(new 
            { 
                ImportantPeopleCount = _importantPeople.Count,
                ImportantPeople = _importantPeople.ToList(),
                Message = "Important people list updated successfully."
            });
        }

        [HttpGet]
        public IActionResult GetImportantPeople()
        {
            return Ok(new 
            { 
                ImportantPeopleCount = _importantPeople.Count,
                ImportantPeople = _importantPeople.ToList()
            });
        }

        [HttpDelete]
        public IActionResult ClearImportantPeople()
        {
            _importantPeople.Clear();
            return Ok("Important people list cleared.");
        }

        public static bool IsImportantMeeting(List<string> attendees)
        {
            return attendees.Any(attendee => _importantPeople.Contains(attendee));
        }
    }

    public class ImportantPeopleRequest
    {
        public List<string> ImportantPeople { get; set; } = new();
    }
}