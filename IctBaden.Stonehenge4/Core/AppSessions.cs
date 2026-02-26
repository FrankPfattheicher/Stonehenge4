using System.Collections.Generic;
using System.Linq;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Core;

public class AppSessions
{
    private readonly List<AppSession> _sessions = [];
    
    public int Count
    {
        get
        {
            lock (_sessions)
            {
                return _sessions.Count;
            }
        }
    }

    public void AddSession(AppSession session)
    {
        lock (_sessions)
        {
            _sessions.Add(session);
        }
    }

    public AppSession? GetSessionById(string? sessionId)
    {
        lock (_sessions)
        {
            return _sessions.Find(s => string.Equals(s.Id, sessionId, System.StringComparison.Ordinal));
        }
    }
    public AppSession? GetSessionByDataResourceId(string dataResourceId)
    {
        lock (_sessions)
        {
            return _sessions.Find(s => string.Equals((s.ViewModel as ActiveViewModel)?.DataResourceId, dataResourceId, System.StringComparison.Ordinal));
        }
    }
    public AppSession? GetSessionByAuthorizeRedirectUrl(string authorizeRedirectUrl)
    {
        lock (_sessions)
        {
            return _sessions.Find(s => string.Equals(s.AuthorizeRedirectUrl, authorizeRedirectUrl, System.StringComparison.Ordinal));
        }
    }

    public AppSession? GetSessionByClientAddressAndUserAgent(string clientAddress, string? userAgent)
    {
        lock (_sessions)
        {
            return _sessions
                .Find(s => string.Equals(s.ClientAddress, clientAddress, System.StringComparison.Ordinal) 
                           && string.Equals(s.UserAgent, userAgent, System.StringComparison.Ordinal));
        }
    }

    public AppSession? GetSessionByNonce(string? nonce)
    {
        lock (_sessions)
        {
            var appSession = _sessions.Find(s => string.Equals(s.Nonce, nonce, System.StringComparison.Ordinal));
            if(appSession != null) appSession.Nonce = string.Empty;
            return appSession;
        }
    }
    
    public void RemoveSessionById(string sessionId)
    {
        lock (_sessions)
        {
            _sessions.RemoveAll(s => string.Equals(s.Id, sessionId, System.StringComparison.Ordinal));
        }
    }

    public AppSession[] GetAllSessions()
    {
        lock (_sessions)
        {
            return _sessions.ToArray();
        }
    }

    public AppSession[] GetTimedOutSessions()
    {
        lock (_sessions)
        {
            var timedOutSessions = _sessions
                .Where(s => s.IsTimedOut)
                .ToArray();
            return timedOutSessions;
        }
    }
}