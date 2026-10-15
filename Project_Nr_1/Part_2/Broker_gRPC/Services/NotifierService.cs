using Broker;
using Grpc.Core;
using GrpcAgent;

namespace Broker_gRPC.Services;

public class NotifierService : Notifier.NotifierBase
{
    private const int PollMs = 500;

    private readonly IPersistentStore _persistentStore;
    private readonly ILogger<NotifierService> _logger;

    public NotifierService(IPersistentStore persistentStore, ILogger<NotifierService> logger)
    {
        _persistentStore = persistentStore;
        _logger = logger;
    }

    public override async Task Listen(
        ListenRequest request,
        IServerStreamWriter<DeliveryMessage> responseStream,
        ServerCallContext context)
    {
        string clientId = GetClientId(context);
        var ct = context.CancellationToken;

        _logger.LogInformation("{Client} is now listening", clientId);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var payload in _persistentStore.GetPendingDeliveries(clientId))
                {
                    await responseStream.WriteAsync(new DeliveryMessage
                    {
                        Id = payload.Id.ToString(),
                        Topic = payload.Topic,
                        Message = payload.Message,
                        CreatedAt = payload.CreatedAtUtc.ToString("o")
                    });

                    _persistentStore.MarkDelivered(payload.Id, clientId);

                    _logger.LogInformation("Delivered {Id} to {Client}", payload.Id, clientId);
                }

                await Task.Delay(PollMs, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // clientul a închis conexiunea
        }

        _logger.LogInformation("{Client} stopped listening", clientId);
    }

    private static string GetClientId(ServerCallContext context)
    {
        if (context.UserState.TryGetValue(AuthInterceptor.UserKey, out var user) && user is string username)
            return username;

        throw new RpcException(new Status(StatusCode.Unauthenticated, "not authenticated"));
    }
}