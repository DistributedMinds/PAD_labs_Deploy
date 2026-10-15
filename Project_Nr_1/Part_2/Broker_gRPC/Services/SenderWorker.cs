<<<<<<< HEAD
﻿
=======
﻿using System.Collections.Concurrent;
using Broker;
using Broker_gRPC.Storage;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcAgent;

namespace Broker_gRPC.Services
{
    public class SenderWorker : IHostedService
    {
        private const int TimeToWait = 500;

        private Timer? _timer;
        private int _running;

        private readonly IPersistentStore _persistentStore;
        private readonly IConnectionStorage _connectionStorage;
        private readonly ILogger<SenderWorker> _logger;
        private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new();

        public SenderWorker(
            IPersistentStore persistentStore,
            IConnectionStorage connectionStorage,
            ILogger<SenderWorker> logger)
        {
            _persistentStore = persistentStore;
            _connectionStorage = connectionStorage;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker started.");
            _timer = new Timer(DoSendWork, null, 0, TimeToWait);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            foreach (var channel in _channels.Values)
            {
                channel.Dispose();
            }

            return Task.CompletedTask;
        }

        private void DoSendWork(object? state)
        {
            // Nu pornim un tur nou cât timp cel precedent încă rulează
            if (Interlocked.Exchange(ref _running, 1) == 1)
                return;

            try
            {
                foreach (var connection in _connectionStorage.GetAllConnected())
                {
                    if (string.IsNullOrEmpty(connection.ClientId))
                        continue;

                    var pending = _persistentStore.GetPendingDeliveries(connection.ClientId);
                    if (pending.Count == 0)
                        continue;

                    var client = new Notifier.NotifierClient(GetChannel(connection.Address));

                    foreach (var payload in pending)
                    {
                        try
                        {
                            var reply = client.Notify(
                                new NotifyRequest { Topic = payload.Topic, Message = payload.Message },
                                deadline: DateTime.UtcNow.AddSeconds(3));

                            if (reply.Success)
                            {
                                _persistentStore.MarkDelivered(payload.Id, connection.ClientId);

                                _logger.LogInformation(
                                    "Delivered message {Id} to {Client}",
                                    payload.Id,
                                    connection.ClientId);
                            }
                        }
                        catch (RpcException e)
                        {
                            _logger.LogError(
                                "Delivery failed to {Address}: {Error}",
                                connection.Address,
                                e.Status.Detail);

                            // Receiver-ul nu răspunde: scoatem conexiunea (revine la următorul sign in)
                            _connectionStorage.Remove(connection.Address);

                            if (_channels.TryRemove(connection.Address, out var dead))
                            {
                                dead.Dispose();
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Worker error");
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        private GrpcChannel GetChannel(string address)
            => _channels.GetOrAdd(address, a => GrpcChannel.ForAddress(a));
    }
}
>>>>>>> origin/main
