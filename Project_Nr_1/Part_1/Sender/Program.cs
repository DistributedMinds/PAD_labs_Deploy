using System.Text;
using System.Xml.Serialization;
using Message_Agent.Common;
using Newtonsoft.Json;
using Sender;

Console.WriteLine("Sender");

var senderSocket = new SenderSocket();
senderSocket.Connect(Settings.Host, Settings.BROKER_PORT);

if (senderSocket.IsConected)
{
    while (true)
    {
        var payLoad = new PayLoad();

        Console.Write("Enter topic: ");
        payLoad.Topic = Console.ReadLine().ToLower();
        Console.Write("Enter message: ");
        payLoad.Message = Console.ReadLine();
        var formatChoice = string.Empty;

        do
        {
            Console.Write("Choose serialization format (1 for JSON, 2 for XML): ");
            formatChoice = Console.ReadLine();
            if (formatChoice != "1" && formatChoice != "2")
            {
                Console.WriteLine("Invalid choice. Choose again");
            }
        } while (formatChoice != "1" && formatChoice != "2");

        string payLoadString;

        if (formatChoice == "1")
        {
            payLoadString = JsonConvert.SerializeObject(payLoad);
        }
        else
        {
            var serializer = new XmlSerializer(typeof(PayLoad));
            using var writer = new StringWriter();
            serializer.Serialize(writer, payLoad);
            payLoadString = writer.ToString();
        }
        
        byte[] data = Encoding.UTF8.GetBytes(payLoadString + "\n");
        senderSocket.Send(data);
    }
}

Console.ReadLine();
