using System.Collections.Concurrent;
using Tailmail.Protos;

namespace Tailmail.Web.Services;

public class MessageStore
{
    private readonly ConcurrentBag<MessageRequest> _messages = new();
    public event Action? OnMessageAdded;

    public void AddMessage(MessageRequest message)
    {
        _messages.Add(message);
        OnMessageAdded?.Invoke();
    }

    public IEnumerable<MessageRequest> GetMessages()
    {
        return _messages.OrderByDescending(m => m.Timestamp);
    }
}
