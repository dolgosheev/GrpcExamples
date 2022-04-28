using Grpc.Core;

using PresentationService;

namespace Server.ServerStream.ServicesInterfaces;

public interface ICacheAsync
{
    Task<bool> TryAddOrUpdateClient(string clientGuid,IServerStreamWriter<MessageResponse> client);
    Task<bool> TryRemoveClient(string clientGuid);
    Task<bool> TryGetClient(string clientGuid);

    Task<bool> SendDataBroadcast(MessageResponse message);
    Task<bool> SendDataToClient(MessageResponse message, string clientGuid);
}