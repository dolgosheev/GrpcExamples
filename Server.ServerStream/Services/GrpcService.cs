using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using PresentationService;

using Server.ServerStream.ServicesInterfaces;

namespace Server.ServerStream.Services;

public class GrpcService : PresentationService.PresentationService.PresentationServiceBase
{
    private readonly ICacheAsync _cache;
    private readonly ILogger<GrpcService> _logger;

    public GrpcService(ILogger<GrpcService> logger, ICacheAsync cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public override async Task RegisterNewClient(RegisterNewClientRequest request,
        IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
    {
        await Task.Yield();
        
        while (!await _cache.TryAddOrUpdateClient(request.Guid, responseStream))
        {
            _logger.LogInformation("Trying to add client {ClientGuid}", request.Guid);
            await Task.Delay(1000);
        }
        _logger.LogInformation("Client {ClientGuid} connected and stream cached", request.Guid);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            await responseStream.WriteAsync(new MessageResponse
            {
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                Ping = new Ping
                {
                    Alive = true
                }
            });
            await Task.Delay(1000);
        }

        while (!await _cache.TryRemoveClient(request.Guid))
        {
            _logger.LogInformation("Trying to add remove {ClientGuid}", request.Guid);
            await Task.Delay(1000);
        }
        _logger.LogInformation("Client {ClientGuid} disconnected and stream disposed", request.Guid);
    }

    public override async Task<SendClientMessageRequestResponse> SendClientMessage(SendClientMessageRequest request,
        ServerCallContext context)
    {
        await Task.Yield();
        
        var defaultReturn = new SendClientMessageRequestResponse
        {
            Status = false,
            Reason = "Fail",
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        if (context.CancellationToken.IsCancellationRequested)
            return defaultReturn;
        
        switch (request.ActionCase)
        {
            case SendClientMessageRequest.ActionOneofCase.TextMessage:
                defaultReturn.Status = true;
                defaultReturn.Reason = "Success";
                
                _logger.LogDebug("Client {ClientGuid} send text message [{Time}]:[{Message}]", request.Guid,
                    request.Time.ToDateTime().ToString("F"), request.TextMessage.Message);
                break;
            case SendClientMessageRequest.ActionOneofCase.VoiceMessage:
                defaultReturn.Status = true;
                defaultReturn.Reason = "Success";
                
                _logger.LogDebug("Client {ClientGuid} send voice message [{Time}]:[{Message}]", request.Guid,
                    request.Time.ToDateTime().ToString("F"), request.VoiceMessage.Message.ToBase64());
                break;
            default:
                _logger.LogError("Have no action");
                defaultReturn.Reason = "Have no action";
                return defaultReturn;
        }
        
        if (!await _cache.TryGetClient(request.Guid))
        {
            defaultReturn.Reason = $"Success | WARNING Client [{request.Guid}] is not registered";
            _logger.LogError("Client {ClientGuid} is not registered",request.Guid);
        }

        return defaultReturn;
    }
}