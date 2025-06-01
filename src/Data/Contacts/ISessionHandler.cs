using AuthChannel.Data.Models;

namespace AuthChannel.Data.Contacts;

public interface ISessionHandler
{
    /// <summary>
    /// Creates a new session or loads the existed session.
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="partnerName"></param>
    /// <returns>The session instance</returns>
    Task<Session> GetOrCreateSessionAsync(string userName, string partnerName);

    /// <summary>
    /// Gets all related sessions of one user.
    /// </summary>
    /// <param name="userName"></param>
    /// <returns>A list of sessions</returns>
    Task<KeyValuePair<string, Session>[]> GetLatestSessionsAsync(string userName);
    /// <summary>  
    /// Retrieves a list of session entities by session ID.  
    /// </summary>  
    /// <param name="sessionId">The unique identifier of the session.</param>  
    /// <returns>A task that represents the asynchronous operation, containing a list of session entities.</returns>  
    Task<List<SessionEntity>> GetSessionBySessionId(string sessionId);
    /// <summary>  
    /// Deletes a user session associated with the specified user and partner.  
    /// </summary>  
    /// <param name="userName">The name of the user.</param>  
    /// <param name="partnerName">The name of the partner.</param>  
    /// <returns>A task that represents the asynchronous operation, returning true if the session was successfully deleted.</returns>  
    Task<bool> DeleteUserSession(string userName, string partnerName);
}
