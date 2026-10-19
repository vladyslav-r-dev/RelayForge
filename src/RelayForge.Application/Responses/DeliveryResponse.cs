using RelayForge.Domain.Enums;

namespace RelayForge.Application.Responses;

public class DeliveryResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid EndpointId { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int AttemptCount { get; set; }
}
