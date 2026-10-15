using Broker;
using Broker_gRPC.Models;
using Broker_gRPC.Storage;
using Grpc.Core;
using GrpcAgent;

namespace Broker_gRPC.Services;

public class AuthService : Authenticator.AuthenticatorBase
{
    private readonly IPersistentStore _persistentStore;
    private readonly IConnectionStorage _connectionStorage;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IPersistentStore persistentStore,
        IConnectionStorage connectionStorage,
        ISessionStore sessionStore,
        ILogger<AuthService> logger)
    {
        _persistentStore = persistentStore;
        _connectionStorage = connectionStorage;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public override Task<AuthReply> SignUp(AuthRequest request, ServerCallContext context)
        => Task.FromResult(HandleAuth(request, isSignUp: true));

    public override Task<AuthReply> SignIn(AuthRequest request, ServerCallContext context)
        => Task.FromResult(HandleAuth(request, isSignUp: false));

    private AuthReply HandleAuth(AuthRequest request, bool isSignUp)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return new AuthReply { Success = false, Info = "username and password are required" };
        }

        bool success = isSignUp
            ? _persistentStore.TryRegister(request.Username, request.Password)
            : _persistentStore.TryAuthenticate(request.Username, request.Password);

        if (!success)
        {
            _logger.LogWarning(
                "Failed {Mode} for '{User}' from {Address}",
                isSignUp ? "signup" : "signin",
                request.Username,
                request.Address);

            return new AuthReply
            {
                Success = false,
                Info = isSignUp ? "username taken" : "invalid credentials"
            };
        }

        // Restaurăm topicurile din DB (ca în Part 1)
        var previousTopics = _persistentStore.GetSubscribedTopics(request.Username);

        var connection = new Connection(request.Address) { ClientId = request.Username };
        connection.Topics.AddRange(previousTopics);
        _connectionStorage.Add(connection);

        string token = _sessionStore.Create(request.Username);

        _logger.LogInformation(
            "{Address} authenticated as '{User}' ({Mode}). Restored topics: {Topics}",
            request.Address,
            request.Username,
            isSignUp ? "signup" : "signin",
            string.Join(",", previousTopics));

        var reply = new AuthReply { Success = true, Info = "Authenticated successfully.", Token = token };
        reply.Topics.AddRange(previousTopics);
        return reply;
    }
}