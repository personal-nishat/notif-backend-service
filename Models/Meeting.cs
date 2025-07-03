namespace push_notif.Models
{
    public class Meeting
    {
        public required string MeetingId { get; set; }
        public required DateTime MeetingStartTime { get; set; }
        public required DateTime MeetingEndTime { get; set; }
        public required List<string> Attendees { get; set; } = new();

        // Override Equals and GetHashCode to ensure no duplicate meetings
        public override bool Equals(object? obj)
        {
            if (obj is Meeting other)
            {
                return MeetingId == other.MeetingId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return MeetingId.GetHashCode();
        }
    }
    public class MeetingChoice
    {
        public required string ConflictId { get; set; }
        public required string SelectedMeetingId { get; set; }
        public required string RejectedMeetingId { get; set; }
        public DateTime ChoiceMadeAt { get; set; } = DateTime.UtcNow;
    }

    public class MeetingChoiceRequest
    {
        public required string ConflictId { get; set; }
        public required string SelectedMeetingId { get; set; }
    }
}