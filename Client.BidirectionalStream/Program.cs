using Grpc.Core;
using Grpc.Net.Client;

using PresentationService;

const string ip = "localhost";
const int port = 30001;

using var channel = GrpcChannel.ForAddress($"http://{ip}:{port}");
var client = new PresentationService.PresentationService.PresentationServiceClient(channel);

var guid = Guid.NewGuid().ToString();

var stream = client.BidirectionalStream();

await Task.Yield();

var request = Task.Run(async () =>
{
    // Register
    var registerMessage = new MessageRequest
    {
        Guid = guid,
        Register = new Register
        {
            Password = "password"
        }
    };
    await TransmitMessageAsync(stream?.RequestStream, registerMessage);

    while (stream != null && !stream.RequestStream.Equals(null))
    {
        // Messaging
        Console.WriteLine("You can send message to server, please type it :");

        var response = Console.ReadLine();
        var message = new MessageRequest
        {
            Guid = guid,
            TextMessage = new TextMessage
            {
                Message = response
            }
        };
        await TransmitMessageAsync(stream.RequestStream, message);
    }
});

var response = ReceiveMessagesAsync(stream?.ResponseStream);

Task.WaitAll(request, response);

async Task TransmitMessageAsync(IClientStreamWriter<MessageRequest>? streamWriter, MessageRequest message)
{
    var tokenSource = new CancellationTokenSource();

    try
    {
        await streamWriter?.WriteAsync(message)!;
    }
    catch (RpcException)
    {
        Console.WriteLine("Connection was aborted");
        tokenSource.Cancel();
    }
    catch (IOException)
    {
        Console.WriteLine("Connection was aborted");
        tokenSource.Cancel();
    }
    catch (Exception)
    {
        Console.WriteLine("Connection was aborted");
        tokenSource.Cancel();
    }
}

async Task ReceiveMessagesAsync(IAsyncStreamReader<MessageResponse>? message)
{
    var tokenSource = new CancellationTokenSource();

    try
    {
        while (await message?.MoveNext()!)
        {
            var current = message.Current;
            var (mType, mData) = GetMessageData(current);
            Console.WriteLine($"Server received message type [{mType}] with data [{mData}]");
        }
    }
    catch (RpcException)
    {
        Console.WriteLine("Connection was aborted");
        tokenSource.Cancel();
    }
    catch (IOException)
    {
        Console.WriteLine("Connection was aborted");
        tokenSource.Cancel();
    }
    catch (Exception)
    {
        Console.WriteLine("Connection was aborted");
        tokenSource.Cancel();
    }
}

Tuple<string, string> GetMessageData(MessageResponse message)
{
    switch (message.ActionCase)
    {
        case MessageResponse.ActionOneofCase.TextMessage:
            //var time = message.TextMessage?.Time ?? null;
            // var msg = time is null
            //     ? message.TextMessage?.Message ?? string.Empty
            //     : string.Format($"[{time.ToDateTime():F}] {message.TextMessage?.Message ?? string.Empty}");
            
            return new Tuple<string, string>(nameof(TextMessage), message.TextMessage?.Message ?? string.Empty);
        case MessageResponse.ActionOneofCase.VoiceMessage:
            return new Tuple<string, string>(nameof(VoiceMessage),
                message.VoiceMessage?.Message?.ToBase64() ?? string.Empty);
        case MessageResponse.ActionOneofCase.Ping:
            return new Tuple<string, string>(nameof(Ping), message.Ping?.Alive.ToString() ?? string.Empty);
    }

    return new Tuple<string, string>("unknown", string.Empty);
}