
namespace RelayForge.Application.DTOs;

public class CreateWebhookEventRequest
{
    public string EventType { get; set; }
    public string Data { get; set; }
}