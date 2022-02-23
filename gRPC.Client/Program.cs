using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;

using PresentationServiceGrpc;

namespace gRPC.Client
{
    internal static class Program
    {
        private static readonly string _ip = "localhost";
        private static readonly int _port = 8001;

        private static PresentationService.PresentationServiceClient _client;

        private static Channel _channel;

        private static void Main()
        {
            _channel = new Channel($"{_ip}:{_port}", ChannelCredentials.Insecure);
            _client = new PresentationService.PresentationServiceClient(_channel);

            var e = _client?.EstablishConnection(new Metadata
            {
                new ("clientid",Guid.NewGuid().ToString())
            });

            Task.Run(async () =>
            {
                while (e is null || !await e.StreamingAsync())
                {
                    Console.WriteLine("Try reconnect...");
                    e = _client?.EstablishConnection(new Metadata
                    {
                        new ("clientId",Guid.NewGuid().ToString())
                    });
                    await Task.Delay(2000);
                }
            });
            
            while (e is not null)
            {
                //Console.Write("Type you client message :");
                var msg = Console.ReadLine();
                e?.Receive(msg);
            }
            
            Console.ReadLine();
        }

        private static async Task<bool> StreamingAsync(this AsyncDuplexStreamingCall<MessageRequest, MessageResponse> e)
        {
            if (e is null) return true;
            await Task.WhenAll(e.Transmit());
            return false;
        }

        private static async Task Receive(this AsyncDuplexStreamingCall<MessageRequest, MessageResponse> e,
            string message)
        {
            var tokenSource = new CancellationTokenSource();

            try
            {
                await e.RequestStream.WriteAsync(new MessageRequest
                {
                    Message = message
                });
            }
            catch (RpcException)
            {
                Console.WriteLine("Connection was aborted");
                tokenSource.Cancel();
            }
            catch (IOException)
            {
                Console.WriteLine("Connection was aborted");
                tokenSource.Cancel();
            }
            catch (Exception)
            {
                Console.WriteLine("Connection was aborted");
                tokenSource.Cancel();
            }
        }

        private static async Task Transmit(this AsyncDuplexStreamingCall<MessageRequest, MessageResponse> e)
        {
            var tokenSource = new CancellationTokenSource();

            try
            {
                while (await e.ResponseStream.MoveNext())
                {
                    var current = e.ResponseStream.Current;
                    Console.WriteLine($"Hello from server : {current.Message}");
                }
            }
            catch (RpcException)
            {
                Console.WriteLine("Connection was aborted");
                tokenSource.Cancel();
            }
            catch (IOException)
            {
                Console.WriteLine("Connection was aborted");
                tokenSource.Cancel();
            }
            catch (Exception)
            {
                Console.WriteLine("Connection was aborted");
                tokenSource.Cancel();
            }
        }
    }
}