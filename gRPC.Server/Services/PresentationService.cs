using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using PresentationServiceGrpc;

namespace gRPC.Server.Services
{
    public class PresentationService : PresentationServiceGrpc.PresentationService.PresentationServiceBase
    {
        private readonly ILogger<PresentationService> _logger;
        private readonly Cache _cache;
        

        public PresentationService(ILogger<PresentationService> logger,Cache cache)
        {
            _logger = logger;
            _cache = cache;

            Task.Run(async () =>
            {
                while (true)
                {
                    //Console.Write("Type server message :");
                    var msg = Console.ReadLine();
                    await ServerToClientsAsync(msg);
                }
            });
        }

        public override async Task EstablishConnection(
            IAsyncStreamReader<MessageRequest> requestStream,
            IServerStreamWriter<MessageResponse> responseStream,
            ServerCallContext context)
        {
            var c2S = ClientToServerAsync(requestStream,responseStream, context);

            _cache._clients.Add(responseStream);
            Console.WriteLine($"Connected new client : {context.RequestHeaders.FirstOrDefault(e=>e.Key == "clientid")?.Value}");
            //var s2C = ServerToClientAsync(responseStream, context);

            await Task.WhenAll(c2S);
        }

        public async Task ServerToClientsAsync(string message)
        {
            foreach (var stream in _cache._clients.Where(stream => stream is not null))
            {
                await stream?.WriteAsync(new MessageResponse
                {
                    Message = message
                });
            }
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

        private async Task ClientToServerAsync(
            IAsyncStreamReader<MessageRequest> requestStream,
            IServerStreamWriter<MessageResponse> responseStream,
            ServerCallContext context)
        {
            try
            {
                while (await requestStream?.MoveNext()! && !context.CancellationToken.IsCancellationRequested)
                {
                    var message = requestStream?.Current;
                    _logger.LogInformation("Client said {Message}", message?.Message);
                }
            }
            catch (IOException)
            {
                _logger.LogWarning("Connection was aborted");
                _cache._clients.Remove(responseStream);
                Console.WriteLine($"Client disconnected : {context.RequestHeaders.FirstOrDefault(e=>e.Key == "clientid")?.Value}");
            }
            catch (RpcException)
            {
                _logger.LogWarning("Connection was aborted");
                _cache._clients.Remove(responseStream);
                Console.WriteLine($"Client disconnected : {context.RequestHeaders.FirstOrDefault(e=>e.Key == "clientid")?.Value}");
            }
            catch (Exception)
            {
                _logger.LogWarning("Connection was aborted");
                _cache._clients.Remove(responseStream);
                Console.WriteLine($"Client disconnected : {context.RequestHeaders.FirstOrDefault(e=>e.Key == "clientid")?.Value}");
            }
        }
    }
}