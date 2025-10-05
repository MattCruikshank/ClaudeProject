namespace Tailmail.Web.Services;

public class NavigationService
{
    private readonly SettingsService _settingsService;
    private readonly MessageStore _messageStore;
    private readonly SentMessageStore _sentMessageStore;

    public NavigationService(SettingsService settingsService, MessageStore messageStore, SentMessageStore sentMessageStore)
    {
        _settingsService = settingsService;
        _messageStore = messageStore;
        _sentMessageStore = sentMessageStore;
    }

    public List<string> GetNavigationItems()
    {
        var items = new List<string> { "" }; // Empty string represents "All" page

        // Get all peer names from settings
        var peerNames = _settingsService.GetSettings().Peers
            .Select(p => p.UserName ?? string.Empty)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        items.AddRange(peerNames);

        // Get all senders from messages that don't match any peer
        var messageSenders = _messageStore.GetMessages()
            .Select(m => m.Sender ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToHashSet();

        var unknownSenders = messageSenders
            .Where(s => !peerNames.Contains(s))
            .OrderBy(s => s)
            .ToList();

        items.AddRange(unknownSenders);

        return items;
    }

    public string GetNextItem(string currentItem, bool moveUp)
    {
        var items = GetNavigationItems();
        var currentIndex = items.IndexOf(currentItem);

        if (currentIndex == -1)
        {
            return currentItem; // Current item not found, don't navigate
        }

        int newIndex;
        if (moveUp)
        {
            newIndex = currentIndex - 1;
            if (newIndex < 0)
            {
                newIndex = items.Count - 1; // Circle to bottom
            }
        }
        else
        {
            newIndex = currentIndex + 1;
            if (newIndex >= items.Count)
            {
                newIndex = 0; // Circle to top
            }
        }

        return items[newIndex];
    }
}
