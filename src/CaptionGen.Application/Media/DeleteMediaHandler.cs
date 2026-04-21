using MediatR;

namespace CaptionGen.Application.Media;

public sealed class DeleteMediaHandler : IRequestHandler<DeleteMediaCommand>
{
    private readonly IMediaAssetRepository _repo;
    private readonly IMediaStorageService _storage;

    public DeleteMediaHandler(IMediaAssetRepository repo, IMediaStorageService storage)
    {
        _repo = repo;
        _storage = storage;
    }

    public async Task Handle(DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        var asset = await _repo.GetByIdAsync(request.MediaId, request.UserId, cancellationToken);
        if (asset is null)
            throw new InvalidOperationException("Media not found.");

        await _storage.DeleteAsync(asset.StoragePath, cancellationToken);
        await _repo.DeleteAsync(asset, cancellationToken);
    }
}
