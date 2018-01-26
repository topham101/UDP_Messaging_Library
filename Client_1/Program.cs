using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UDP_Messaging;

namespace Client_1
{
    class Program
    {
        private static ManualResetEvent mre = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            MessagingHost server = new MessagingHost();
            server.Connection.ConnectionStatusChanged += WriteState;
            server.MessageReceived += Write;
            server.ConnectionClosed += WriteDisconnect;
            var remoteEP = new IPEndPoint(IPAddress.Parse("192.168.1.69"), 25566);
            server.Connect(remoteEP);

            mre.WaitOne();

            string input = "";
            while (input != "q")
            {
                input = Console.ReadLine();
                server.Send(input);
            }

            server.Stop();
            Console.ReadKey();
        }

        private static void Write(string x)
        {
            Console.WriteLine("Received: " + x);
        }

        private static void WriteState(ConnectionState x)
        {
            Console.WriteLine(x.ToString());
            if (x == ConnectionState.Connected)
                mre.Set();
        }

        private static void WriteDisconnect(ConnectionLossType x)
        {
            Console.WriteLine("Disconnected: " + x.ToString());
        }
    }
}
