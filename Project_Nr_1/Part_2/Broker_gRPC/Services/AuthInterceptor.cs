using Broker_gRPC.Storage;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Broker_gRPC.Services;

public class AuthInterceptor : Interceptor
{
    public const string UserKey = "username";

    private static readonly string[] ProtectedPrefixes =
    {
        "/GrpcAgent.Subscriber/",
        "/GrpcAgent.Notifier/"
    };
    private const string BearerPrefix = "Bearer ";

    private readonly ISessionStore _sessions;

    public AuthInterceptor(ISessionStore sessions)
    {
        _sessions = sessions;
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        return continuation(request, context);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        return continuation(request, responseStream, context);
    }

    private void Authenticate(ServerCallContext context)
    {
        if (!ProtectedPrefixes.Any(p => context.Method.StartsWith(p, StringComparison.Ordinal)))
            return;

        var header = context.RequestHeaders.GetValue("authorization");

        string? token = header != null &&
                        header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header.Substring(BearerPrefix.Length)
            : null;

        string? username = token == null ? null : _sessions.GetUsername(token);
        if (username == null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "not authenticated"));

        context.UserState[UserKey] = username;
    }
}