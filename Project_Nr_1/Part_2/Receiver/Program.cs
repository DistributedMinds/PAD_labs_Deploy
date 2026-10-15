using Grpc.Core;
using Grpc.Net.Client;
using GrpcAgent;
using Message_Agent.Common;

var settings = Settings.Load();

var channel = GrpcChannel.ForAddress(settings.BrokerUrl, new GrpcChannelOptions
{
    HttpHandler = new SocketsHttpHandler
    {
        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
        EnableMultipleHttp2Connections = true
    }
});
var authClient = new Authenticator.AuthenticatorClient(channel);
var subscriberClient = new Subscriber.SubscriberClient(channel);
var notifierClient = new Notifier.NotifierClient(channel);

string? token = null;
var topics = new List<string>();
var receivedMessages = new List<(DateTime At, string Topic, string Text)>();
var messagesLock = new object();
var cts = new CancellationTokenSource();

Console.WriteLine("========== RECEIVER ==========");

// ---------- Autentificare (ca în Part 1) ----------
bool authenticated = false;

while (!authenticated)
{
    Console.WriteLine();
    Console.Write("Sign up sau sign in? (1 = sign up, 2 = sign in): ");
    string? choice = Console.ReadLine();

    if (choice != "1" && choice != "2")
    {
        Console.WriteLine("Optiune invalida. Alege 1 sau 2.");
        continue;
    }

    Console.Write("Username: ");
    string username = Console.ReadLine() ?? "";

    Console.Write("Password: ");
    string password = Console.ReadLine() ?? "";

    var result = Authenticate(username, password, isSignUp: choice == "1");

    if (result.Success)
    {
        authenticated = true;
        Console.WriteLine(result.Message);

        // pornim ascultarea: brokerul trimite mesajele pe aceasta conexiune
        _ = Task.Run(() => ListenLoop(cts.Token));
    }
    else
    {
        Console.WriteLine($"Autentificare esuata: {result.Message}. Incearca din nou.");
    }
}

// ---------- Meniu ----------
bool running = true;

while (running)
{
    Console.WriteLine("1. Subscribe to topic");
    Console.WriteLine("2. Unsubscribe from topic");
    Console.WriteLine("3. View subscribed topics");
    Console.WriteLine("4. View received messages");
    Console.WriteLine("5. Exit");
    Console.WriteLine("===========================");
    Console.Write("Choose an option: ");

    string? option = Console.ReadLine();

    switch (option)
    {
        case "1":
            Console.Write("Enter topic: ");
            string topic = (Console.ReadLine() ?? "").ToLower();

            if (topics.Contains(topic))
            {
                Console.WriteLine($"Already subscribed to topic: {topic}");
                break;
            }

            if (!string.IsNullOrWhiteSpace(topic))
            {
                Subscribe(topic);
            }
            break;

        case "2":
            Console.Write("Enter topic: ");
            string topicToUnsub = (Console.ReadLine() ?? "").ToLower();

            if (!topics.Contains(topicToUnsub))
            {
                Console.WriteLine($"Not subscribed to topic: {topicToUnsub}");
                break;
            }

            if (!string.IsNullOrWhiteSpace(topicToUnsub))
            {
                Unsubscribe(topicToUnsub);
            }
            break;

        case "3":
            Console.WriteLine();
            Console.WriteLine("----- SUBSCRIBED TOPICS -----");

            if (topics.Count == 0)
            {
                Console.WriteLine("No subscribed topics.");
            }
            else
            {
                foreach (var t in topics)
                {
                    Console.WriteLine($"- {t}");
                }
            }

            Console.WriteLine("------------------------------");
            break;

        case "4":
            Console.WriteLine();
            Console.WriteLine("===== RECEIVED MESSAGES =====");

            List<(DateTime At, string Topic, string Text)> snapshot;
            lock (messagesLock)
            {
                snapshot = receivedMessages.ToList();
            }

            if (snapshot.Count == 0)
            {
                Console.WriteLine("No messages received.");
            }
            else
            {
                foreach (var group in snapshot.GroupBy(x => x.Topic))
                {
                    Console.WriteLine();
                    Console.WriteLine($"[{group.Key}]");

                    foreach (var msg in group)
                    {
                        Console.WriteLine($"- [{msg.At:HH:mm:ss}] {msg.Text}");
                    }
                }
            }

            Console.WriteLine("=============================");
            break;

        case "5":
            cts.Cancel();
            running = false;
            break;

        default:
            Console.WriteLine("Invalid option.");
            break;
    }
}

// ---------- Funcții locale ----------

(bool Success, string Message) Authenticate(string username, string password, bool isSignUp)
{
    var request = new AuthRequest
    {
        Username = username,
        Password = password
    };

    try
    {
        var reply = isSignUp ? authClient.SignUp(request) : authClient.SignIn(request);

        if (!reply.Success)
        {
            return (false, reply.Info);
        }

        token = reply.Token;

        foreach (var t in reply.Topics)
        {
            if (!topics.Contains(t))
                topics.Add(t);
        }

        return (true, "Authenticated successfully.");
    }
    catch (RpcException ex)
    {
        return (false, ex.Status.Detail);
    }
    catch (Exception ex)
    {
        return (false, ex.Message);
    }
}

Metadata AuthHeaders()
{
    return new Metadata { { "authorization", $"Bearer {token}" } };
}

void Subscribe(string topic)
{
    var request = new SubscribeRequest
    {
        Topic = topic
    };

    try
    {
        var reply = subscriberClient.Subscribe(request, AuthHeaders());
        if (reply.Success)
        {
            Console.WriteLine($"Successfully subscribed to topic: {topic}");
            topics.Add(topic);
        }
        else
        {
            Console.WriteLine($"Failed to subscribe: {reply.Info}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while subscribing: {ex.Message}");
    }
}

void Unsubscribe(string topic)
{
    var request = new SubscribeRequest
    {
        Topic = topic
    };

    try
    {
        var reply = subscriberClient.Unsubscribe(request, AuthHeaders());
        if (reply.Success)
        {
            Console.WriteLine($"Successfully unsubscribed from topic: {topic}");
            topics.Remove(topic);
        }
        else
        {
            Console.WriteLine($"Failed to unsubscribe: {reply.Info}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while unsubscribing: {ex.Message}");
    }
}

async Task ListenLoop(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            using var call = notifierClient.Listen(new ListenRequest(), AuthHeaders(), cancellationToken: ct);

            await foreach (var item in call.ResponseStream.ReadAllAsync(ct))
            {
                lock (messagesLock)
                {
                    receivedMessages.Add((DateTime.Now, item.Topic, item.Message));
                }
                Console.WriteLine($"\n[NEW] {item.Topic}: {item.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && ct.IsCancellationRequested)
        {
            return;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            Console.WriteLine("Sesiune expirata (brokerul a fost repornit). Reporneste Receiver-ul.");
            return;
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"Conexiune pierduta ({ex.StatusCode}), reincerc...");
        }

        try { await Task.Delay(2000, ct); }
        catch (OperationCanceledException) { return; }
    }
}