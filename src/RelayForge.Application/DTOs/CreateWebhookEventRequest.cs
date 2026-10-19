
namespace RelayForge.Application.DTOs;

public class CreateWebhookEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
