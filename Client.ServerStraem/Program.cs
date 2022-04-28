using Google.Protobuf.WellKnownTypes;

using Grpc.Core;
using Grpc.Net.Client;

using PresentationService;

const string ip = "localhost";
const int port = 30001;

using var channel = GrpcChannel.ForAddress($"http://{ip}:{port}");
var client = new PresentationService.PresentationService.PresentationServiceClient(channel);

var guid = Guid.NewGuid().ToString();

await Task.Yield();

var registerObj = new RegisterNewClientRequest
{
    Guid = guid,
    Register = new Register
    {
        Password = "passw0rd"
    }
};

var app = Task.Run(async () =>
{
    await Task.Yield();
    while (true)
    {
        Console.WriteLine($"Alive {DateTime.UtcNow:F}");
        await Task.Delay(1000 * 10);
    }
},CancellationToken.None);

var response = Task.Run(async () =>
{
    await Task.Yield();
    while (true)
    {
        var register = client.RegisterNewClient(registerObj);

        try
        {
            await foreach (var message in register.ResponseStream.ReadAllAsync())
            {
                var (mType, mData) = GetMessageData(message);
                if(!mType.Equals(nameof(Ping)))
                    Console.WriteLine($"Server received message type [{mType}] with data [{mData}]");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Could not receive messages | Exception {e.Message} [type {typeof(Exception)}] | InnerException {e.InnerException?.Message}");
        }

        register.Dispose();
        await Task.Delay(1000 * 3);
        Console.WriteLine("RECONNECTION");
    }
},CancellationToken.None);

var request = Task.Run(async () =>
{
    await Task.Yield();
    while (true)
    {
        var message = Console.ReadLine();
        Console.WriteLine($"Input : {message}");

        await TransmitTextMessage(message ?? string.Empty);
    }
},CancellationToken.None);

Console.WriteLine($"App started at {DateTime.UtcNow:F}");

Task.WaitAll(app, response,request);

async Task TransmitTextMessage(string message)
{
    try
    {
        var response = await client.SendClientMessageAsync(new SendClientMessageRequest
        {
            Guid = guid,
            Time = Timestamp.FromDateTime(DateTime.UtcNow),
            TextMessage = new TextMessage
            {
                Message = message
            }
        });

        Console.WriteLine($"response : {response}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Could not sent message | Exception {e.Message} [type {typeof(Exception)}] | InnerException {e.InnerException?.Message}");
    }
}

Tuple<string, string> GetMessageData(MessageResponse message)
{
    switch (message.ActionCase)
    {
        case MessageResponse.ActionOneofCase.TextMessage:
            return new Tuple<string, string>(nameof(TextMessage), message.TextMessage?.Message ?? string.Empty);
        case MessageResponse.ActionOneofCase.VoiceMessage:
            return new Tuple<string, string>(nameof(VoiceMessage), message.VoiceMessage?.Message?.ToBase64() ?? string.Empty);
        case MessageResponse.ActionOneofCase.Ping:
            return new Tuple<string, string>(nameof(Ping), message.Ping?.Alive.ToString() ?? string.Empty);
    }

    return new Tuple<string, string>("unknown", string.Empty);
}





// async Task ReceiveMessagesAsync(CancellationToken cancel)
// {
//     
//     while (!cancel.IsCancellationRequested)
//     {
//         registerStream = client?.RegisterNewClient(registerObj);
//
//         try
//         {
//             Console.WriteLine($"c {registerStream?.ResponseStream?.Current} m {await registerStream?.ResponseStream?.MoveNext()!}");
//             while (await registerStream?.ResponseStream?.MoveNext()!)
//             {
//                 var current = registerStream.ResponseStream.Current;
//                 var (mType, mData) = GetMessageData(current);
//                 Console.WriteLine($"Server received message type [{mType}] with data [{mData}]");
//             }
//             
//             // await foreach (var current in  registerStream?.ResponseStream.ReadAllAsync()!)
//             // {
//             //     var (mType, mData) = GetMessageData(current);
//             //     Console.WriteLine($"Server received message type [{mType}] with data [{mData}]");
//             // }
//         }
//         catch (RpcException)
//         {
//             Console.WriteLine("Connection was aborted");
//             registerStream = null;
//             registerStream = client?.RegisterNewClient(registerObj);
//         }
//         catch (IOException)
//         {
//             Console.WriteLine("Connection was aborted");
//             registerStream = null;
//             registerStream = client?.RegisterNewClient(registerObj);
//         }
//         catch (Exception)
//         {
//             Console.WriteLine("Connection was aborted");
//             registerStream = null;
//             registerStream = client?.RegisterNewClient(registerObj);
//         }
//
//         await Task.Delay(1000 * 3);
//         Console.WriteLine("Reconnect...");
//     }
// }
//
// Tuple<string, string> GetMessageData(MessageResponse message)
// {
//     switch (message.ActionCase)
//     {
//         case MessageResponse.ActionOneofCase.TextMessage:
//             return new Tuple<string, string>(nameof(TextMessage), message.TextMessage?.Message ?? string.Empty);
//         case MessageResponse.ActionOneofCase.VoiceMessage:
//             return new Tuple<string, string>(nameof(VoiceMessage), message.VoiceMessage?.Message?.ToBase64() ?? string.Empty);
//         case MessageResponse.ActionOneofCase.Ping:
//             return new Tuple<string, string>(nameof(Ping), message.Ping?.Alive.ToString() ?? string.Empty);
//     }
//
//     return new Tuple<string, string>("unknown", string.Empty);
// }