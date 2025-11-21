namespace Shared.Contracts;

public class BusinessException : Exception
{
    public string? Code { get; }
    public BusinessException(string message, string? code = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}
