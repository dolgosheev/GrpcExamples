using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Grpc.Core;

using PresentationServiceGrpc;

namespace gRPC.Server.Services
{
    public class Cache
    {
        public List<IServerStreamWriter<MessageResponse>> _clients = new();

        public Cache()
        {
            // Task.Run(async () =>
            // {
            //     while (true)
            //     {
            //         Console.WriteLine($"Count of clients - {_clients.Count}");
            //         await Task.Delay(1000);
            //     }
            // });
        }
    }
}