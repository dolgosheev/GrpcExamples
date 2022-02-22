using System.IO;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using PresentationServiceGrpc;

namespace gRPC.Server.Services
{
    public class PresentationService : PresentationServiceGrpc.PresentationService.PresentationServiceBase
    {
        private readonly ILogger<PresentationService> _logger;

        public PresentationService(ILogger<PresentationService> logger)
        {
            _logger = logger;
        }

        public override Task<MessageResponse> WrapMessage(MessageRequest request, ServerCallContext context)
        {
            return Task.FromResult(new MessageResponse
            {
                Message = $"response to : {request?.Message.ToUpper()}"
            });
        }

        public override async Task EstablishConnection(
            IAsyncStreamReader<MessageRequest> requestStream,
            IServerStreamWriter<MessageResponse> responseStream,
            ServerCallContext context)
        {
            var c2S = ClientToServerAsync(requestStream, context);

            var s2C = ServerToClientAsync(responseStream, context);

            await Task.WhenAll(c2S, s2C);
        }

        private static async Task ServerToClientAsync(IServerStreamWriter<MessageResponse> responseStream,
            ServerCallContext context)
        {
            var ping = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await responseStream.WriteAsync(new MessageResponse
                {
                    Message = $"Sever said HI {++ping} times"
                });
                await Task.Delay(1000);
            }
        }

        private async Task ClientToServerAsync(IAsyncStreamReader<MessageRequest> requestStream,
            ServerCallContext context)
        {
            try
            {
                while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
                {
                    var message = requestStream.Current;
                    _logger.LogInformation("Client said {Message}", message.Message);
                }
            }
            catch (IOException)
            {
                _logger.LogWarning("Connection was aborted");
            }
        }
    }
}