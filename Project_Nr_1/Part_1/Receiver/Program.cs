using Message_Agent.Common;
using System.Collections.Generic;

namespace Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========== RECEIVER ==========");

            var receiverSocket = new ReceiverSocket();

            receiverSocket.Connect(Settings.Host, Settings.BROKER_PORT);

            bool authenticated = false;

            while (!authenticated)
            {
                Console.WriteLine();
                Console.Write("Sign up sau sign in? (1 = sign up, 2 = sign in): ");
                string choice = Console.ReadLine();

                if (choice != "1" && choice != "2")
                {
                    Console.WriteLine("Opțiune invalidă. Alege 1 sau 2.");
                    continue;
                }

                Console.Write("Username: ");
                string username = Console.ReadLine();

                Console.Write("Password: ");
                string password = Console.ReadLine();

                var result = receiverSocket.Authenticate(username, password, isSignUp: choice == "1");

                if (result.Success)
                {
                    authenticated = true;
                    Console.WriteLine(result.Message);
                }
                else
                {
                    Console.WriteLine($"Autentificare eșuată: {result.Message}. Încearcă din nou.");
                }
            }

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

                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        Console.Write("Enter topic: ");
                        string topic = Console.ReadLine().ToLower();

                        if (!string.IsNullOrWhiteSpace(topic))
                        {
                            receiverSocket.Subscribe(topic);
                        }
                        break;

                    case "2":
                        Console.Write("Enter topic: ");
                        string topicToUnsub = Console.ReadLine().ToLower();

                        if (!string.IsNullOrWhiteSpace(topicToUnsub))
                        {
                            receiverSocket.Unsubscribe(topicToUnsub);
                        }
                        break;

                    case "3":
                        Console.WriteLine();
                        Console.WriteLine("----- SUBSCRIBED TOPICS -----");

                        var topics = receiverSocket.GetTopics();

                        if (topics.Count == 0)
                        {
                            Console.WriteLine("No subscribed topics.");
                        }
                        else
                        {
                            foreach (string subscribedTopic in topics)
                            {
                                Console.WriteLine($"- {subscribedTopic}");
                            }
                        }

                        Console.WriteLine("------------------------------");
                        break;

                    case "4":
                        Console.WriteLine();
                        Console.WriteLine("===== RECEIVED MESSAGES =====");

                        var messages = PayloadHandler.GetMessages();

                        if (messages.Count == 0)
                        {
                            Console.WriteLine("No messages received.");
                        }
                        else
                        {
                            foreach (var topicMessages in messages)
                            {
                                Console.WriteLine();
                                Console.WriteLine($"[{topicMessages.Key}]");

                                foreach (string message in topicMessages.Value)
                                {
                                    Console.WriteLine($"- {message}");
                                }
                            }
                        }

                        Console.WriteLine("=============================");
                        break;

                    case "5":
                        running = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }

            Console.WriteLine("Receiver closed.");
        }
    }
}