namespace LeakTestService.Exceptions;

public class NoMatchingDataException : Exception
{
    public NoMatchingDataException(string message) : base(message)
    {
    }
}