 using Broker;
using Grpc.Core;
using GrpcAgent;
using Message_Agent.Common.Models;

namespace Broker_gRPC.Services;

public class PublisherService : Publisher.PublisherBase
{
    private readonly ILogger<PublisherService> _logger;
    private readonly IPersistentStore _persistentStore;

    public PublisherService(
        ILogger<PublisherService> logger,
        IPersistentStore persistentStore)
    {
        _logger = logger;
        _persistentStore = persistentStore;
    }

    public override Task<PublishReply> PublishMessage(
        PublishRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
              "Message received from {Peer}. Topic: {Topic}",
                context.Peer,
                request.Topic);

        var payload = new PayLoad
        {
            Topic = request.Topic,
            Message = request.Message
        };
        
        _persistentStore.AddMessageWithDeliveries(payload);

        return Task.FromResult(new PublishReply
        {
            Success = true,
            Info = "Message sent"
        });
    }
}