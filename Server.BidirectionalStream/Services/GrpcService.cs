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
        await ReceiveMessagesAsync(requestStream, responseStream, context);
    }

    private async Task ReceiveMessagesAsync(
        IAsyncStreamReader<MessageRequest> requestStream,
        IServerStreamWriter<MessageResponse> responseStream,
        ServerCallContext context)
    {
        await Task.Yield();
        
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

                        while (!await _cache.TryAddOrUpdateClient(responseStream,clientGuid))
                        {
                            _logger.LogWarning("Trying to add or update client {Guid} | attempt {Attempt}", clientGuid, ++attempts);
                            await Task.Delay(1000);
                        }
                        break;
                    case MessageRequest.ActionOneofCase.TextMessage:
                        _logger.LogInformation("[message from client] Client Guid {Guid} | Received message : {Message}",
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
                _logger.LogWarning("Trying to remove client | attempt {Attempt}", ++attempts);
                await Task.Delay(1000);
            }

            _logger.LogWarning(
                "An error has been occured | Called method {Caller} | Exception type {ExceptionType} | Exception {Exception} | InnerException {InnerException}",
                MethodBase.GetCurrentMethod()?.Name, typeof(Exception), e.Message, e.InnerException?.Message);
        }
    }
}