using System.Collections.Generic;
using System.Linq;

namespace IctBaden.Stonehenge.Core;

public static class AppSessions
{
    private static readonly List<AppSession> Sessions = [];
    public static int Count
    {
        get
        {
            lock (Sessions)
            {
                return Sessions.Count;
            }
        }
    }

    public static void AddSession(AppSession session)
    {
        lock (Sessions)
        {
            Sessions.Add(session);
        }
    }

    public static AppSession? GetSessionById(string? sessionId)
    {
        lock (Sessions)
        {
            return Sessions.FirstOrDefault(s => s.Id == sessionId);
        }
    }
    
    public static void RemoveSessionById(string sessionId)
    {
        lock (Sessions)
        {
            Sessions.RemoveAll(s => s.Id == sessionId);
        }
    }

    public static AppSession[] GetAllSessions()
    {
        lock (Sessions)
        {
            return Sessions.ToArray();
        }
    }

    public static AppSession[] GetTimedOutSessions()
    {
        lock (Sessions)
        {
            var timedOutSessions = Sessions
                .Where(s => s.IsTimedOut)
                .ToArray();
            return timedOutSessions;
        }
    }
}