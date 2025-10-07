using Grpc.Net.Client;
using Tailmail.Protos;
using Microsoft.Extensions.Logging;

namespace Tailmail.Web.Services;

public class MessageSender
{
    private readonly SettingsService _settingsService;
    private readonly SentMessageStore _sentMessageStore;
    private readonly ILogger<MessageSender> _logger;

    public MessageSender(SettingsService settingsService, SentMessageStore sentMessageStore, ILogger<MessageSender> logger)
    {
        _settingsService = settingsService;
        _sentMessageStore = sentMessageStore;
        _logger = logger;
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
            _logger.LogInformation("Attempting to send message to {PeerName} at {Server}", peerName, peer.Server);

            // Create gRPC channel with HTTP/2 support for insecure connections
            var httpHandler = new HttpClientHandler();
            var channelOptions = new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            };

            using var channel = GrpcChannel.ForAddress(peer.Server, channelOptions);
            var client = new MessageService.MessageServiceClient(channel);

            var response = await client.SendMessageAsync(request);

            if (response.Success)
            {
                _logger.LogInformation("Message sent successfully to {PeerName}", peerName);
                // Add to sent messages store on success
                _sentMessageStore.AddMessage(request);
                return true;
            }
            else
            {
                _logger.LogWarning("Message delivery failed to {PeerName}: server returned success=false", peerName);
                // Add to sent messages store with error message
                request.ErrorMessage = "Message not delivered";
                _sentMessageStore.AddMessage(request);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to {PeerName} at {Server}", peerName, peer.Server);
            // Add to sent messages store with error message
            request.ErrorMessage = $"Message not delivered: {ex.Message}";
            _sentMessageStore.AddMessage(request);
            return false;
        }
    }
}
