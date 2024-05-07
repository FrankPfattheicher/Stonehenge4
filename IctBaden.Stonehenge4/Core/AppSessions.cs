using System.Collections.Generic;
using System.Linq;

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
            return _sessions.FirstOrDefault(s => s.Id == sessionId);
        }
    }
    
    public void RemoveSessionById(string sessionId)
    {
        lock (_sessions)
        {
            _sessions.RemoveAll(s => s.Id == sessionId);
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