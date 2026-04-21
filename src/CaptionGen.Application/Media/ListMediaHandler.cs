using MediatR;

namespace CaptionGen.Application.Media;

public sealed class ListMediaHandler : IRequestHandler<ListMediaQuery, IReadOnlyList<MediaAssetDto>>
{
    private readonly IMediaAssetRepository _repo;
    private readonly IMediaStorageService _storage;

    public ListMediaHandler(IMediaAssetRepository repo, IMediaStorageService storage)
    {
        _repo = repo;
        _storage = storage;
    }

    public async Task<IReadOnlyList<MediaAssetDto>> Handle(ListMediaQuery request, CancellationToken cancellationToken)
    {
        var items = await _repo.ListByUserAsync(request.UserId, cancellationToken);

        return items
            .Select(a => new MediaAssetDto(
                a.Id,
                a.Type,
                _storage.BuildPublicUrl(a.StoragePath),
                a.CreatedAtUtc))
            .ToList();
    }
}
