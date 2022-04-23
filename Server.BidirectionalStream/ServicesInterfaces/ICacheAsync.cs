using Grpc.Core;

using PresentationService;

namespace Server.BidirectionalStream.ServicesInterfaces;

public interface ICacheAsync
{
    Task<bool> TryAddOrUpdateClient(IServerStreamWriter<MessageResponse> client,string clientGuid);
    Task<bool> TryRemoveClient(IServerStreamWriter<MessageResponse> client);
}