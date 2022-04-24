using System.Reflection;

using Grpc.Core;

using PresentationService;

using Server.BidirectionalStream.ServicesInterfaces;

namespace Server.BidirectionalStream.Services;

public class GrpcService : PresentationService.PresentationService.PresentationServiceBase
{
    private readonly ICacheAsync _cache;
    private readonly ILogger<GrpcService> _logger;

    public GrpcService(ILogger<GrpcService> logger, ICacheAsync cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public override async Task BidirectionalStream(
        IAsyncStreamReader<MessageRequest> requestStream,
        IServerStreamWriter<MessageResponse> responseStream,
        ServerCallContext context)
    {
        await Task.Yield();

        var request = Task.Run(async () =>
        {
            while (!responseStream.Equals(null))
            {
                Console.WriteLine("You can send message broadcast (or private guid| message ), please type it :");
                var response = Console.ReadLine();

                var split = response?.Split('|') ?? Array.Empty<string>();

                var privateMessage = split.Length > 1
                    ? split[0].Trim()
                    : null;

                var messageData = split.Length > 1
                    ? split[1].Trim()
                    : split[0].Trim();

                var message = new MessageResponse
                {
                    TextMessage = new TextMessage
                    {
                        Message = messageData
                    }
                };
                var result = string.IsNullOrWhiteSpace(privateMessage)
                    ? await _cache.SendDataBroadcast(message)
                    : await _cache.SendDataToClient(message, privateMessage);

                if (result)
                    _logger.LogInformation("Message [{Data}] was sent", response);
            }
        });

        var response = ReceiveMessagesAsync(requestStream, responseStream, context);

        Task.WaitAll(request, response);
    }

    private async Task ReceiveMessagesAsync(
        IAsyncStreamReader<MessageRequest> requestStream,
        IServerStreamWriter<MessageResponse> responseStream,
        ServerCallContext context)
    {
        await Task.Yield();
        var tokenSource = new CancellationTokenSource();

        try
        {
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                var message = requestStream.Current;

                switch (message.ActionCase)
                {
                    case MessageRequest.ActionOneofCase.Register:
                        var clientGuid = message.Guid ?? default(Guid).ToString();
                        var attempts = 1;

                        while (!await _cache.TryAddOrUpdateClient(responseStream, clientGuid))
                        {
                            if (attempts >= 3)
                            {
                                tokenSource.Cancel();
                                break;
                            }

                            _logger.LogWarning("Trying to add or update client {Guid} | attempt {Attempt}", clientGuid,
                                ++attempts);
                            await Task.Delay(1000);
                        }

                        break;
                    case MessageRequest.ActionOneofCase.TextMessage:
                        _logger.LogInformation(
                            "[message from client] | Received message : {Message} from client {Guid}",
                            message.Guid, message.TextMessage?.Message);
                        break;
                    case MessageRequest.ActionOneofCase.VoiceMessage:
                        break;
                    case MessageRequest.ActionOneofCase.Ping:
                        break;
                }
            }
        }
        catch (Exception e)
        {
            var attempts = 1;

            while (!await _cache.TryRemoveClient(responseStream))
            {
                if (attempts >= 3)
                {
                    tokenSource.Cancel();
                    break;
                }

                _logger.LogWarning("Trying to remove client | attempt {Attempt}", ++attempts);
                await Task.Delay(1000);
            }

            _logger.LogWarning(
                "An error has been occured | Called method {Caller} | Exception type {ExceptionType} | Exception {Exception} | InnerException {InnerException}",
                MethodBase.GetCurrentMethod()?.Name, typeof(Exception), e.Message, e.InnerException?.Message);
        }
    }
}