﻿using AuthChannel.Data.Contacts;
using AuthChannel.Data.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AuthChannel.Data.SessionHandler.InMemorySessionStorage;

public class InMemorySessionStorage : ISessionHandler
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Session>> _sessionDictionary;

    public InMemorySessionStorage()
    {
        _sessionDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, Session>>();
    }

    public Task<Session> GetOrCreateSessionAsync(string userName, string partnerName)
    {
        if (!_sessionDictionary.ContainsKey(userName))
        {
            _sessionDictionary.TryAdd(userName, new ConcurrentDictionary<string, Session>());
        }
        if (!_sessionDictionary.ContainsKey(partnerName))
        {
            _sessionDictionary.TryAdd(partnerName, new ConcurrentDictionary<string, Session>());
        }

        _sessionDictionary.TryGetValue(userName, out var userSessions);
        Debug.Assert(userSessions != null, nameof(userSessions) + " != null");

        if (userSessions.TryGetValue(partnerName, out var session))
        {
            return Task.FromResult(session);
        }

        session = new Session(Guid.NewGuid().ToString());
        userSessions.TryAdd(partnerName, session);
        _sessionDictionary[partnerName].TryAdd(userName, session);

        return Task.FromResult(session);
    }

    public Task<KeyValuePair<string, Session>[]> GetLatestSessionsAsync(string userName)
    {
        if (!_sessionDictionary.TryGetValue(userName, out var userSessions))
        {
            _sessionDictionary.TryAdd(userName, new ConcurrentDictionary<string, Session>());
            _sessionDictionary.TryGetValue(userName, out userSessions);
        }

        Debug.Assert(userSessions != null, nameof(userSessions) + " != null");

        var sortedSessions = new SortedDictionary<string, Session>(userSessions);
        return Task.FromResult(sortedSessions.ToArray());
    }

    public Task<bool> DeleteUserSession(string userName, string partnerName)
    {
        throw new NotImplementedException();
    }

    public Task<List<SessionEntity>> GetSessionBySessionId(string sessionId)
    {
        throw new NotImplementedException();
    }
}
