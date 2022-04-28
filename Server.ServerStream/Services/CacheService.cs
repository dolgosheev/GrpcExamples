using System.Collections.Concurrent;
using System.Reflection;

using Grpc.Core;

using PresentationService;

using Server.ServerStream.ServicesInterfaces;

namespace Server.ServerStream.Services;

public class CacheService : ICacheAsync
{
    private readonly ConcurrentDictionary<string,IServerStreamWriter<MessageResponse>> _clients = new();
    private readonly object _locker = new();
    private readonly ILogger<CacheService> _logger;

    public CacheService(ILogger<CacheService> logger)
    {
        _logger = logger;
    }

    public Task<bool> TryAddOrUpdateClient(string clientGuid,IServerStreamWriter<MessageResponse> client)
    {
        lock (_locker)
        {
            try
            {
                var result = _clients.AddOrUpdate(clientGuid,client,  (_, _) => client);

                _logger.LogInformation("Client {Guid} stream has been added or updated", clientGuid);
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

    public Task<bool> TryRemoveClient(string clientGuid)
    {
        lock (_locker)
        {
            try
            {
                var result = _clients.TryRemove(clientGuid, out var _);

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

    public Task<bool> TryGetClient(string clientGuid)
    {
        lock (_locker)
        {
            try
            {
                var result = _clients.TryGetValue(clientGuid,out var client);

                if (result)
                {
                    _logger.LogInformation("Client {Guid} stream exist", clientGuid);
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
                foreach (var (clientGuid,stream) in _clients)
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
                var stream = _clients[clientGuid];
                
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