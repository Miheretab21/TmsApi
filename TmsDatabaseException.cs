namespace TmsApi;

public class TmsDatabaseException : Exception
{
    public TmsDatabaseException(string message) : base(message) { }
    public TmsDatabaseException(string message, Exception inner) : base(message, inner) { }
}
