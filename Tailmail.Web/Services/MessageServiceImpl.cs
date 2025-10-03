using Grpc.Core;
using Tailmail.Protos;

namespace Tailmail.Web.Services;

public class MessageServiceImpl : MessageService.MessageServiceBase
{
    private readonly ILogger<MessageServiceImpl> _logger;
    private readonly MessageStore _messageStore;

    public MessageServiceImpl(ILogger<MessageServiceImpl> logger, MessageStore messageStore)
    {
        _logger = logger;
        _messageStore = messageStore;
    }

    public override Task<MessageResponse> SendMessage(MessageRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received message from {Sender}: {Content}", request.Sender, request.Content);

        _messageStore.AddMessage(request);

        return Task.FromResult(new MessageResponse
        {
            Success = true,
            Message = "Message received"
        });
    }
}
