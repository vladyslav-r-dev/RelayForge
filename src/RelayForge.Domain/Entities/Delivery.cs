using RelayForge.Domain.Enums;

namespace RelayForge.Domain.Entities;
public class Delivery
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid EndpointId { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int AttemptCount { get; private set; }
    

    public Delivery(Guid eventId, Guid endpointId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event ID cannot be empty.",
                nameof(eventId));
        }

        if (endpointId == Guid.Empty)
        {
            throw new ArgumentException(
                "Endpoint ID cannot be empty.",
                nameof(endpointId));
        }

        Id = Guid.NewGuid();
        EventId = eventId;
        EndpointId = endpointId;
        Status = DeliveryStatus.Pending;
        AttemptCount = 0;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsDelivered()
    {
        if (Status != DeliveryStatus.Pending)
        {
        throw new InvalidOperationException(
            "Only pending deliveries can be marked as delivered.");
        }

        Status = DeliveryStatus.Delivered;
    }

    public void RegisterAttempt()
    {
        if(Status == DeliveryStatus.Pending)
        {
            AttemptCount++;
        }
        else
        {
            throw new InvalidOperationException(
                "Only pending deliveries can register an attempt.");
        }
    }

    public void RegisterFailure(int maxAttempts)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        if (Status != DeliveryStatus.Pending || AttemptCount == 0)
        {
            throw new InvalidOperationException(
                "Cannot register failure for this delivery.");
        }

        if (AttemptCount >= maxAttempts)
        {
            Status = DeliveryStatus.Failed;
        }
    }
}