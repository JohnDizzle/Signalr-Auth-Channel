﻿using AuthChannel.Data.Contacts;
using AuthChannel.Data.Models;
using System.Collections.Concurrent;

namespace AuthChannel.Data.MessageHandler.InMemoryMessageStorage;

public class InMemoryMessageStorage : IMessageHandler
{
    private readonly ConcurrentDictionary<string, SessionMessage> _messageDictionary;

    public InMemoryMessageStorage()
    {
        _messageDictionary = new ConcurrentDictionary<string, SessionMessage>();
    }

    public Task<string> AddNewMessageAsync(string sessionId, Message message)
    {
        if (!_messageDictionary.TryGetValue(sessionId, out var sessionMessage))
        {
            _messageDictionary.TryAdd(sessionId, new SessionMessage());
            sessionMessage = _messageDictionary[sessionId];
        }

        var sequenceId = sessionMessage.TryAddMessage(message);

        return Task.FromResult(sequenceId.ToString());
    }

    public async Task UpdateMessageAsync(string sessionId, string sequenceId, string messageStatus)
    {
        if (!_messageDictionary.TryGetValue(sessionId, out var sessionMessage))
        {
            throw new Exception("Session not found!");
        }

        await sessionMessage.TryUpdateMessage(int.Parse(sequenceId), messageStatus);

        return;
    }

    public Task<List<Message>> LoadHistoryMessageAsync(string sessionId)
    {
        if (!_messageDictionary.TryGetValue(sessionId, out var sessionMessage))
        {
            _messageDictionary.TryAdd(sessionId, new SessionMessage());
            return Task.FromResult(new List<Message>());
        }

        var result = new List<Message>(sessionMessage.Messages.Values.ToList());
        result.Sort();

        return Task.FromResult(result);
    }
    public async Task<List<Message>> LoadMessagesByDateRangeAsync(string sessionId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (!_messageDictionary.TryGetValue(sessionId, out var sessionMessage))
        {
            _messageDictionary.TryAdd(sessionId, new SessionMessage());
            return new List<Message>();
        }

        var filteredMessages = sessionMessage.Messages.Values
            .Where(message => message.SendTime >= startDate && message.SendTime <= endDate)
            .OrderBy(message => message.SendTime)
            .ToList();

        return await Task.FromResult(filteredMessages);
    }

    Task<Message?> IMessageHandler.UpdateMessageAsync(string sessionId, string sequenceId, string messageStatus)
    {
        throw new NotImplementedException();
    }

    internal class SessionMessage
    {
        public int LastSequenceId;

        public ConcurrentDictionary<int, Message> Messages { get; set; }

        public SessionMessage()
        {
            LastSequenceId = -1;
            Messages = new ConcurrentDictionary<int, Message>();
        }

        public int TryAddMessage(Message message)
        {
            var sequenceId = Interlocked.Increment(ref LastSequenceId);
            message.SequenceId = sequenceId.ToString();
            Messages.TryAdd(sequenceId, message);

            return sequenceId;
        }

        public async Task TryUpdateMessage(int sequenceId, string messageStatus)
        {
            var retry = 0;
            const int MAX_RETRY = 10;

            while (retry < MAX_RETRY)
            {
                Messages.TryGetValue(sequenceId, out var message);
                var newMessage = message;
                newMessage.MessageStatus = messageStatus;

                if (Messages.TryUpdate(sequenceId, newMessage, message))
                {
                    return;
                }

                ++retry;
                await Task.Delay(new Random().Next(10, 100));
            }

            throw new Exception("Fail to update messages");
        }
    }
}
