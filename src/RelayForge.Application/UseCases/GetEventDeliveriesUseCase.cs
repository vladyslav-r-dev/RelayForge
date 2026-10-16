using RelayForge.Application.Interfaces;
    
public class GetEventDeliveriesUseCase(IDeliveryRepository deliveryRepository)
{
    private readonly IDeliveryRepository _deliveryRepository = deliveryRepository;
    public async Task<IEnumerable<DeliveryResponse>> ExecuteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var deliveries = await _deliveryRepository.GetByEventIdAsync(eventId, cancellationToken);
        return deliveries.Select(d => new DeliveryResponse
        {
            Id = d.Id,
            EventId = d.EventId,
            EndpointId = d.EndpointId,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            AttemptCount = d.AttemptCount
        });
    }
}