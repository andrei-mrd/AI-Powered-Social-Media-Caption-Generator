using System.ComponentModel.DataAnnotations;

namespace CaptionGen.Api.Contracts.Captions;

public sealed class ImproveCaptionRequestDto
{
    [Required]
    public string Caption { get; init; } = "";
    [Required]
    public string Platform { get; init; } = "";
    [Required]
    public string Tone { get; init; } = "";
    public string Language { get; init; } = "en";
    public string Goal { get; init; } = "engagement";
}
