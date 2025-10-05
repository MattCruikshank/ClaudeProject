using Grpc.Net.Client;
using Tailmail.Protos;

namespace Tailmail.Web.Services;

public class MessageSender
{
    private readonly SettingsService _settingsService;

    public MessageSender(SettingsService settingsService)
    {
        _settingsService = settingsService;
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

        try
        {
            // Create gRPC channel
            using var channel = GrpcChannel.ForAddress(peer.Server);
            var client = new MessageService.MessageServiceClient(channel);

            // Create and send the message
            var request = new MessageRequest
            {
                Sender = settings.UserName ?? "Anonymous",
                Content = messageContent,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var response = await client.SendMessageAsync(request);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }
}
