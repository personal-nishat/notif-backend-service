using Microsoft.AspNetCore.Mvc;
using push_notif.Models;
using push_notif.Services;

namespace push_notif.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private static readonly List<PushSubscription> _subscriptions = [];

        [HttpPost]
        public IActionResult Register([FromBody] SubscriptionDto subscription)
        {
            var model = new PushSubscription
            {
                Endpoint = subscription.Endpoint,
                Keys = new SubscriptionKeys
                {
                    P256dh = subscription.Keys.P256dh,
                    Auth = subscription.Keys.Auth
                }
            };
            _subscriptions.Add(model);
            Console.WriteLine($"Registered subscription: {model.Endpoint}");
            return Ok("Subscription registered.");
        }

        public static List<PushSubscription> GetAll() => _subscriptions;
        
    }
}