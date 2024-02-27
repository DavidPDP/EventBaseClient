namespace EventBaseClient.Exceptions;

public class UnHealthyServiceException : Exception
{
    public UnHealthyServiceException(string message) : base(message)
    {
    }

    public UnHealthyServiceException(string message, Exception inner) : base(message, inner)
    {
    }
}
