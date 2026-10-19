using RelayForge.Application.DTOs;
using RelayForge.Application.Interfaces;
using RelayForge.Application.Security;
using RelayForge.Domain.Entities;

namespace RelayForge.Application.UseCases;

public class CreateEndpointUseCase(IEndpointRepository endpointRepository, IUnitOfWork unitOfWork, TrustedReceivers trustedReceivers)
{
    private readonly IEndpointRepository _endpointRepository = endpointRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Guid> ExecuteAsync(CreateEndpointRequest request, CancellationToken cancellationToken)
    {
        if (!trustedReceivers.IsAllowed(request.Url))
            throw new ArgumentException("URL must exactly match a configured trusted HTTPS receiver.");

        var endpoint = new Endpoint(request.Url);

        _endpointRepository.Add(endpoint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return endpoint.Id;
    }
}
