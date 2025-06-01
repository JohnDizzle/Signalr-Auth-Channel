using AuthChannel.Data.Models;

namespace AuthChannel.Data.Contacts;

public interface IAzureDataTableSessionStorage
{
    // Summary of methods in IAzureDataTableSessionStorage interface

    /// <summary>
    /// Retrieves an existing session for the specified user and partner.
    /// If no session exists, creates a new one.
    /// </summary>
    /// <param name="userName">The username associated with the session.</param>
    /// <param name="partnerName">The partner name associated with the session.</param>
    /// <returns>A Task representing the asynchronous operation, containing the Session object.</returns>
    Task<Session> GetOrCreateSessionAsync(string userName, string partnerName);
    /// <summary>
    /// Fetches the latest sessions associated with the specified user.
    /// </summary>
    /// <param name="userName">The username whose latest sessions are to be retrieved.</param>
    /// <returns>A Task representing the asynchronous operation, containing an array of key-value pairs (string, Session).</returns>
    Task<KeyValuePair<string, Session>[]> GetLatestSessionsAsync(string userName);
    /// <summary>
    /// Retrieves session details based on the provided session ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session to retrieve.</param>
    /// <returns>A Task representing the asynchronous operation, containing a list of SQL_SESSION_ENTITY objects.</returns>
    Task<List<SQL_SESSION_ENTITY>> GetSessionBySessionId(string sessionId);
    /// <summary>
    /// Deletes the session associated with the specified user and partner.
    /// </summary>
    /// <param name="userName">The username associated with the session to delete.</param>
    /// <param name="partnerName">The partner name associated with the session to delete.</param>
    /// <returns>A Task representing the asynchronous operation, containing a boolean indicating success or failure.</returns>
    Task<bool> DeleteUserSession(string userName, string partnerName);
}

