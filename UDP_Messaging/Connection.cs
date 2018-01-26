using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace UDP_Messaging
{
    /// <summary>
    /// A connection instance. Provides UDP functionality through the APIs.
    /// </summary>
    public class Connection
    {
        private UdpClient sender;
        private UdpClient receiver;
        private readonly IPEndPoint localEndPoint;
        private IPEndPoint blankEP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
        private IPEndPoint remoteEndPoint;

        private ConnectionState connectionState = ConnectionState.Offline;

        public ConnectionState ConnectionState
        {
            get
            {
                return connectionState;
            }
            internal set
            {
                if (value != connectionState)
                {
                    ConnectionStatusChanged?.Invoke(value);
                    connectionState = value;
                }
            }
        }
        public event StatusChangedEvent ConnectionStatusChanged;

        internal Connection()
        {
            localEndPoint = new IPEndPoint(GetLocalIPAddress(), 25566);
        }
        internal Connection(int port)
        {
            localEndPoint = new IPEndPoint(GetLocalIPAddress(), port);
        }
        internal Connection(IPEndPoint localEP)
        {
            localEndPoint = localEP;
        }

        internal void Setup()
        {
            receiver = CreateUDPClient();
            sender = CreateUDPClient();
        }
        internal void Initialize(IPEndPoint remoteEP)
        {
            remoteEndPoint = remoteEP;

            sender.Connect(remoteEP);
            receiver.Connect(remoteEP);
        }
        internal void UnInitialize()
        {
            sender?.Dispose();
            receiver?.Dispose();
            sender = null;
            receiver = null;
            remoteEndPoint = null;
        }

        internal void Send(string message)
        {
            byte[] messageBytes = HammingEncoder.EncodeBits(Encoding.ASCII.GetBytes(message));
            sender.Send(messageBytes, messageBytes.Length);
        }
        internal string Receive()
        {
            return Encoding.ASCII.GetString(
                        HammingEncoder.DecodeBits(
                            receiver.Receive(ref blankEP)));
        }
        internal string Receive(ref IPEndPoint tempEP)
        {
            return Encoding.ASCII.GetString(HammingEncoder.DecodeBits(receiver.Receive(ref tempEP)));
        }
        internal int Available()
        {
            return receiver != null ? receiver.Available : -1;
        }

        private UdpClient CreateUDPClient()
        {
            var client = new UdpClient
            {
                ExclusiveAddressUse = false
            };
            client.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);
            client.Client.Bind(localEndPoint);
            return client;
        }
        private static IPAddress GetLocalIPAddress()
        {
            using (Socket socket = new Socket(
                                        AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address;
            }
        }
    }
}
