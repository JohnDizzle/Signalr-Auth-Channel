﻿using AuthChannel.Data.Models;

namespace AuthChannel.Data.Contacts;

public interface IMessageHandler
{
    /// <summary>
    /// Add a new messageStatus to the storage.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="message"></param>
    /// <returns>The sequenceId of the new messageStatus.</returns>
    Task<string> AddNewMessageAsync(string sessionId, Message message);

    /// <summary>
    /// Update an existed messageStatus in the storage.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="sequenceId"></param>
    /// <param name="messageStatus"></param>
    Task<Message?> UpdateMessageAsync(string sessionId, string sequenceId, string messageStatus);

    /// <summary>
    /// Selects the messages from startSequenceId to endSequenceId (both are included).
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="startSequenceId"></param>
    /// <param name="endSequenceId"></param>
    /// <returns>A list of messages sorted by the sequenceId</returns>
    Task<List<Message>> LoadHistoryMessageAsync(string sessionId);
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns>A list of messages sorted by the date span and session</returns>
    Task<List<Message>> LoadMessagesByDateRangeAsync(string sessionId, DateTimeOffset startDate, DateTimeOffset endDate);
}
