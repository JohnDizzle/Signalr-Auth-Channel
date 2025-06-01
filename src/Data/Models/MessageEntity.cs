using Azure;

namespace AuthChannel.Data.Models;

public class MessageEntity : Azure.Data.Tables.ITableEntity
{
    public string RowKey { get; set; } = default!;

    public string PartitionKey { get; set; } = default!;

    public ETag ETag { get; set; } = default!;

    public DateTimeOffset? Timestamp { get; set; } = default!;
    public string? SenderName { get; set; }

    public DateTimeOffset SendTime { get; set; }

    public string? MessageContent { get; set; }

    public string? MessageStatus { get; set; }

    public MessageEntity() { }

    public MessageEntity(string pkey, string rkey, Message message)
    {
        PartitionKey = pkey;
        RowKey = rkey;
        SenderName = message.SenderName;
        SendTime = message.SendTime;
        MessageContent = message.MessageContent;
        MessageStatus = message.MessageStatus;
    }

    public void UpdateMessageStatus(string messageStatus)
    {
        MessageStatus = messageStatus;
    }

    public Message ToMessage()
    {
        return new Message(SenderName!, SendTime.ToLocalTime(), MessageContent!, MessageStatus!)
        {
            SequenceId = RowKey
        };
    }
}


