namespace LeakTestService.Exceptions;

public class LeakTestRepositoryException : Exception
{
    public LeakTestRepositoryException(string message, Exception exception) : base(message, exception)
    {
    }
}