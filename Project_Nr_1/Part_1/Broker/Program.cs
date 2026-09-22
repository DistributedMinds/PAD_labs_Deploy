using Message_Agent.Common;

namespace Broker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Info("Broker started.");
            Console.WriteLine("Broker");

            BrokerSocket socket = new BrokerSocket();
            socket.Start(Settings.BindAddress, Settings.BROKER_PORT);

            var worker = new Worker();
            Task.Factory.StartNew(worker.DoSendMessageWork, TaskCreationOptions.LongRunning);

            await Task.Delay(Timeout.Infinite);
        }
    }
}