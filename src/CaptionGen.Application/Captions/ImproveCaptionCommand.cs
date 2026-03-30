using MediatR;

namespace CaptionGen.Application.Captions;

public sealed record ImproveCaptionCommand(
    string Caption,
    string Platform,
    string Tone,
    string Language,
    string Goal) : IRequest<CaptionImprovementResult>;

public sealed class ImproveCaptionHandler : IRequestHandler<ImproveCaptionCommand, CaptionImprovementResult>
{
    private readonly IAiCaptionService _ai;

    public ImproveCaptionHandler(IAiCaptionService ai)
    {
        _ai = ai;
    }

    public Task<CaptionImprovementResult> Handle(ImproveCaptionCommand request, CancellationToken cancellationToken)
    {
        // Validation is enforced by the MediatR pipeline (ImproveCaptionCommandValidator).
        // Normalize values for consistent forwarding to the AI service.
        var caption = (request.Caption ?? string.Empty).Trim();
        var platform = (request.Platform ?? string.Empty).Trim().ToLowerInvariant();
        var tone = (request.Tone ?? string.Empty).Trim().ToLowerInvariant();
        var language = (request.Language ?? string.Empty).Trim().ToLowerInvariant();
        var goal = (request.Goal ?? string.Empty).Trim().ToLowerInvariant();

        return _ai.ImproveAsync(caption, platform, tone, language, goal, cancellationToken);
    }
}
