using RelayForge.Application.Interfaces;

namespace RelayForge.Application.UseCases;

public class ActivateEndpointUseCase(IEndpointRepository endpointRepository, IUnitOfWork unitOfWork)
{
    private readonly IEndpointRepository _endpointRepository = endpointRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task ExecuteAsync(Guid endpointId, CancellationToken cancellationToken)
    {
        var endpoint = await _endpointRepository.GetByIdAsync(endpointId, cancellationToken);

        if (endpoint is null)
            throw new KeyNotFoundException($"Endpoint {endpointId} not found.");

        endpoint.Activate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
