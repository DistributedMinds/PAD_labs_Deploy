
using Broker_gRPC.Services;
using Broker_gRPC.Logging;
using Broker_gRPC.Storage;
using Broker;
using Message_Agent.Common;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var settings = Settings.Load();
var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    // Mod cloud: Fly face TLS, noi primim HTTP/2 necriptat
    builder.WebHost.ConfigureKestrel(o =>
        o.ListenAnyIP(int.Parse(port), l => l.Protocols = HttpProtocols.Http2));
}
else
{
 
    builder.WebHost.UseUrls(settings.BrokerUrl);
}
=======
builder.Logging.AddProvider(new FileLoggerProvider(
    Path.Combine(builder.Environment.ContentRootPath, "Logs", "broker.log")));

builder.WebHost.UseUrls(settings.BrokerUrl);
>>>>>>> origin/main
builder.Services.AddGrpc(o => o.Interceptors.Add<AuthInterceptor>());
builder.Services.AddSingleton<IPersistentStore, PersistentStore>();
builder.Services.AddSingleton<IConnectionStorage, ConnectionStorage>();
builder.Services.AddSingleton<ISessionStore, SessionStore>();


var app = builder.Build();

app.MapGrpcService<AuthService>();
app.MapGrpcService<PublisherService>();
app.MapGrpcService<SubscriberService>();
app.MapGrpcService<NotifierService>();

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Broker started."));

app.Run(); 