namespace AuthChannel.Data.Models;

public class Session
{
    public string SessionId { get; }

    public Session(string sessionId)
    {
        SessionId = sessionId;
    }
}
