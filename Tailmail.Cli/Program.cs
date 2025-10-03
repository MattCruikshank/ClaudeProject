using Grpc.Net.Client;
using Tailmail.Protos;

if (args.Length < 2)
{
    Console.WriteLine("Usage: Tailmail.Cli <sender> <message>");
    return;
}

var sender = args[0];
var message = string.Join(" ", args.Skip(1));

using var channel = GrpcChannel.ForAddress("http://localhost:5245", new GrpcChannelOptions
{
    HttpHandler = new System.Net.Http.HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    }
});
var client = new MessageService.MessageServiceClient(channel);

var request = new MessageRequest
{
    Sender = sender,
    Content = message,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
};

try
{
    var response = await client.SendMessageAsync(request);
    if (response.Success)
    {
        Console.WriteLine("Message sent successfully!");
    }
    else
    {
        Console.WriteLine("Failed to send message");
    }

    if (response.HasMessage)
    {
        Console.WriteLine(response.Message);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
