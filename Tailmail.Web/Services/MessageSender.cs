using Grpc.Net.Client;
using Tailmail.Protos;

namespace Tailmail.Web.Services;

public class MessageSender
{
    private readonly SettingsService _settingsService;
    private readonly SentMessageStore _sentMessageStore;

    public MessageSender(SettingsService settingsService, SentMessageStore sentMessageStore)
    {
        _settingsService = settingsService;
        _sentMessageStore = sentMessageStore;
    }

    public async Task<bool> SendMessageToPeer(string peerName, string messageContent)
    {
        var settings = _settingsService.GetSettings();

        // Find the peer by name
        var peer = settings.Peers.FirstOrDefault(p => p.UserName == peerName);
        if (peer == null || string.IsNullOrEmpty(peer.Server))
        {
            return false;
        }

        // Create the message
        var request = new MessageRequest
        {
            Sender = settings.UserName ?? "Anonymous",
            Content = messageContent,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Recipient = peerName
        };

        try
        {
            // Create gRPC channel
            using var channel = GrpcChannel.ForAddress(peer.Server);
            var client = new MessageService.MessageServiceClient(channel);

            var response = await client.SendMessageAsync(request);

            if (response.Success)
            {
                // Add to sent messages store on success
                _sentMessageStore.AddMessage(request);
                return true;
            }
            else
            {
                // Add to sent messages store with error message
                request.ErrorMessage = "Message not delivered";
                _sentMessageStore.AddMessage(request);
                return false;
            }
        }
        catch
        {
            // Add to sent messages store with error message
            request.ErrorMessage = "Message not delivered";
            _sentMessageStore.AddMessage(request);
            return false;
        }
    }
}
