using RelayForge.Application.DTOs;
using RelayForge.Application.Interfaces;
using RelayForge.Domain.Entities;

namespace RelayForge.Application.UseCases;
public class CreateEndpointUseCase(IEndpointRepository endpointRepository)
{
    private readonly IEndpointRepository _endpointRepository = endpointRepository;

    public async Task<Guid> ExecuteAsync(CreateEndpointRequest request, CancellationToken cancellationToken)
    {
        var endpoint = new Endpoint(request.Url);

        await _endpointRepository.AddAsync(endpoint, cancellationToken);

        return endpoint.Id;
    }
}