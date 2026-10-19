using RelayForge.Application.Interfaces;
using RelayForge.Application.Responses;

namespace RelayForge.Application.UseCases;

public class GetEndpointUseCase(IEndpointRepository endpointRepository)
{
    private readonly IEndpointRepository _endpointRepository = endpointRepository;

    public async Task<EndpointResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var endpoint = await _endpointRepository.GetByIdAsync(id, cancellationToken);
        if (endpoint == null)
        {
            throw new KeyNotFoundException("Endpoint not found.");
        }

        return new EndpointResponse
        {
            Id = endpoint.Id,
            Url = endpoint.Url,
            IsActive = endpoint.IsActive,
            CreatedAt = endpoint.CreatedAt
        };
    }
}
