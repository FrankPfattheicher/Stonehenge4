using System;
using IctBaden.Stonehenge.Core;

namespace IctBaden.Stonehenge.Extension;

public class SessionExpandedProvider : IExpandedProvider
{
    private readonly AppSession _session;

    public SessionExpandedProvider(AppSession session)
    {
        _session = session;
    }
    public bool GetExpanded(string id) => (bool) Convert.ChangeType(_session[$"expanded{id}"] ?? false, typeof(bool));
    public void SetExpanded(string id, bool expanded) => _session[$"expanded{id}"] = expanded;
}