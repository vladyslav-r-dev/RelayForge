namespace RelayForge.Domain.Entities
{
    public class WebhookEvent
    {
        public Guid Id { get; private set; }
        public string EventType { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Data { get; private set; }

        public WebhookEvent(string eventType, string data)
        {
            if (string.IsNullOrWhiteSpace(eventType))
                throw new ArgumentException("Event type cannot be null or empty.", nameof(eventType));

            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentException("Data cannot be null or empty.", nameof(data));

            Id = Guid.NewGuid();
            EventType = eventType;
            CreatedAt = DateTimeOffset.UtcNow;
            Data = data;
        }
    }
}