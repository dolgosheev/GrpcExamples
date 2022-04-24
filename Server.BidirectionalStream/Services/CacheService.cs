using System.Collections.Concurrent;
using System.Reflection;

using Grpc.Core;

using PresentationService;

using Server.BidirectionalStream.ServicesInterfaces;

namespace Server.BidirectionalStream.Services;

public class CacheService : ICacheAsync
{
    private readonly ConcurrentDictionary<IServerStreamWriter<MessageResponse>, string> _clients = new();
    private readonly object _locker = new();
    private readonly ILogger<CacheService> _logger;

    public CacheService(ILogger<CacheService> logger)
    {
        _logger = logger;
    }

    public Task<bool> TryAddOrUpdateClient(IServerStreamWriter<MessageResponse> client, string clientGuid)
    {
        lock (_locker)
        {
            try
            {
                var result = _clients.AddOrUpdate(client, clientGuid, (_, _) => clientGuid);

                _logger.LogInformation("Client {Guid} stream has been added or updated", result);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "An error has been occured | Called method {Caller} | Exception type {ExceptionType} | Exception {Exception} | InnerException {InnerException}",
                    MethodBase.GetCurrentMethod()?.Name, typeof(Exception), e.Message, e.InnerException?.Message);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> TryRemoveClient(IServerStreamWriter<MessageResponse> client)
    {
        lock (_locker)
        {
            try
            {
                var result = _clients.TryRemove(client, out var clientGuid);

                if (result)
                {
                    _logger.LogInformation("Client {Guid} stream has been removed", clientGuid);
                    return Task.FromResult(true);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "An error has been occured | Called method {Caller} | Exception type {ExceptionType} | Exception {Exception} | InnerException {InnerException}",
                    MethodBase.GetCurrentMethod()?.Name, typeof(Exception), e.Message, e.InnerException?.Message);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> SendDataBroadcast(MessageResponse message)
    {
        lock (_locker)
        {
            try
            {
                foreach (var (stream, clientGuid) in _clients)
                {
                    var status = stream.WriteAsync(message);
                    if (status is {IsCompleted: true})
                        _logger.LogInformation("Message for client {Guid} was sent", clientGuid);
                }

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "An error has been occured | Called method {Caller} | Exception type {ExceptionType} | Exception {Exception} | InnerException {InnerException}",
                    MethodBase.GetCurrentMethod()?.Name, typeof(Exception), e.Message, e.InnerException?.Message);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> SendDataToClient(MessageResponse message, string clientGuid)
    {
        lock (_locker)
        {
            try
            {
                var stream = _clients
                    .FirstOrDefault(s => s.Value.Equals(clientGuid)).Key;

                var status = stream.WriteAsync(message);

                if (status.IsCompleted)
                {
                    _logger.LogInformation("Message for client {Guid} was sent", clientGuid);
                    return Task.FromResult(true);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "An error has been occured | Called method {Caller} | Exception type {ExceptionType} | Exception {Exception} | InnerException {InnerException}",
                    MethodBase.GetCurrentMethod()?.Name, typeof(Exception), e.Message, e.InnerException?.Message);
            }
        }

        return Task.FromResult(false);
    }
}