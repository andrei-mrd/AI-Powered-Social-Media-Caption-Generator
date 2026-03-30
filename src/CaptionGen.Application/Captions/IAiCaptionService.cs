namespace CaptionGen.Application.Captions;

public interface IAiCaptionService
{
    Task<CaptionGenerationResult> GenerateAsync(
        string description,
        string platform,
        string tone,
        int count,
        CaptionGenerationOptions options,
        CancellationToken cancellationToken = default);

    Task<CaptionImprovementResult> ImproveAsync(
        string caption,
        string platform,
        string tone,
        string language,
        string goal,
        CancellationToken cancellationToken = default);
}
