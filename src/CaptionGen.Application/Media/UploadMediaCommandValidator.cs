using FluentValidation;

namespace CaptionGen.Application.Media;

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "video/mp4",
        "video/quicktime"
    };

    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File too large. Limit is {MaxFileSizeBytes / (1024 * 1024)} MB.");
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Unsupported media type. Allowed: jpeg, png, webp, mp4.");
        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("File content is required.");
        RuleFor(x => x.FileName)
            .Must(fn => !string.IsNullOrWhiteSpace(Path.GetExtension(fn)))
            .WithMessage("Filename must have an extension.");
    }
}
