using RelayForge.Application.Interfaces;

namespace RelayForge.Application.UseCases;
public class DeactivateEndpointUseCase(IEndpointRepository endpointRepository)
{
    private readonly IEndpointRepository _endpointRepository = endpointRepository;

    public async Task ExecuteAsync(Guid endpointId, CancellationToken cancellationToken)
    {
        var endpoint = await _endpointRepository.GetByIdAsync(endpointId, cancellationToken);

        if (endpoint is null)
            throw new KeyNotFoundException($"Endpoint {endpointId} not found.");

        endpoint.Deactivate();
        
        await _endpointRepository.UpdateAsync(endpoint, cancellationToken);
    }
}