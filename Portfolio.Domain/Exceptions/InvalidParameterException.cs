namespace Portfolio.Domain.Exceptions;

public class InvalidParameterException : Exception
{
    public InvalidParameterException() { }

    public InvalidParameterException(string message) : base(message)
    {
    }

    public InvalidParameterException(string message, Exception inner) : base(message, inner)
    {
    }
}
