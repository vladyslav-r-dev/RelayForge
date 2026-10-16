public class WebhookEventResponse
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Data { get; set; } = string.Empty;
}