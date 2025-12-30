namespace LexoRank;

public class LexoException : Exception
{
    public LexoException()
    {
    }

    public LexoException(string message) : base(message)
    {
    }

    public LexoException(string message, Exception innerException) : base(message, innerException)
    {
    }
}