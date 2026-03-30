namespace CaptionGen.Application.Captions;

public sealed class AiServiceException : Exception
{
    public int? StatusCode { get; }

    public bool IsClientError => StatusCode is >= 400 and < 500;

    public AiServiceException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
