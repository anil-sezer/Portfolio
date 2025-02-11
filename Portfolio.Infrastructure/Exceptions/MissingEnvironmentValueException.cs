namespace Portfolio.Infrastructure.Exceptions;

public class MissingEnvironmentValueException : Exception
{
    public MissingEnvironmentValueException() { }

    public MissingEnvironmentValueException(string message) : base(message)
    {
    }

    public MissingEnvironmentValueException(string message, Exception inner) : base(message, inner)
    {
    }
}