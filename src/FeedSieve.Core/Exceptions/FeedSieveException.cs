namespace FeedSieve.Core.Exceptions;

public class FeedSieveException: Exception
{
    public FeedSieveException()
    {
    }
    public FeedSieveException(string message) : base(message)
    {
    }
    public FeedSieveException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
