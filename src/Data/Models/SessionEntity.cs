using Azure;

namespace AuthChannel.Data.Models;


public class SQL_SESSION_ENTITY : Azure.Data.Tables.ITableEntity
{
    public string RowKey { get; set; } = default!;

    public string PartitionKey { get; set; } = default!;

    public string? SessionId { get; set; }

    public ETag ETag { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;
    public SQL_SESSION_ENTITY() { }

    public SQL_SESSION_ENTITY(string pkey, string rkey)
    {
        PartitionKey = pkey;
        RowKey = rkey;
    }

    public SQL_SESSION_ENTITY(string pkey, string rkey, Session session)
    {
        PartitionKey = pkey;
        RowKey = rkey;
        SessionId = session.SessionId;
    }

    public Session ToSession()
    {
        return new Session(SessionId!);
    }
}
public class SessionEntity : SQL_SESSION_ENTITY
{
    public new string? SessionId { get; set; }

    public SessionEntity() { }

    public SessionEntity(string pkey, string rkey)
    {
        PartitionKey = pkey;
        RowKey = rkey;
    }

    public SessionEntity(string pkey, string rkey, Session session)
    {
        PartitionKey = pkey;
        RowKey = rkey;
        SessionId = session.SessionId;
    }

    public new Session ToSession()
    {
        return new Session(SessionId!);
    }
}
