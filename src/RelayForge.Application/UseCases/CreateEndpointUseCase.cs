using RelayForge.Application.DTOs;
using RelayForge.Application.Interfaces;
using RelayForge.Domain.Entities;

namespace RelayForge.Application.UseCases;
public class CreateEndpointUseCase(IEndpointRepository endpointRepository, IUnitOfWork unitOfWork)
{
    private readonly IEndpointRepository _endpointRepository = endpointRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Guid> ExecuteAsync(CreateEndpointRequest request, CancellationToken cancellationToken)
    {
        var endpoint = new Endpoint(request.Url);

        _endpointRepository.Add(endpoint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return endpoint.Id;
    }
}