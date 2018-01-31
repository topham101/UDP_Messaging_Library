using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UDP_Messaging
{
    public delegate void MessageEvent(string message);
    public delegate void StatusChangedEvent(ConnectionState NewState);
    public delegate void ConnectionClosedEvent(ConnectionLossType message);
    public delegate void ConnectionIssuesEvent(ConnectionIssue connectionIssue);

    /// <summary>
    /// A simple UDP messaging library for point to point communications between two clients.
    /// </summary>
    public class MessagingHost
    {
        private readonly int? ctorPort = null;
        private readonly IPEndPoint ctorEP = null;

        private ConnectionManager connectionManager;
        
        #region Public APIs
        public MessagingHost()
        {
            Connection = new Connection();
            connectionManager = new ConnectionManager(Connection);
        }
        public MessagingHost(int port)
        {
            ctorPort = port;
            Connection = new Connection(port);
            connectionManager = new ConnectionManager(Connection);
        }
        public MessagingHost(IPEndPoint localEndPoint)
        {
            ctorEP = localEndPoint;
            Connection = new Connection(localEndPoint);
            connectionManager = new ConnectionManager(Connection);
        }

        public void Start() // Starts listening for incoming connections
        {
            if (Connection.ConnectionState != ConnectionState.Offline)
                throw new ConnectionException(
                    "The existing connection must be closed before another can begin.");
            
            // Listen -> Handshake
            Task.Run(() => {
                connectionManager.Listen();
            });
        }

        public void Connect(IPEndPoint remoteEP) // Connects to a Remote End Point
        {
            if (Connection.ConnectionState != ConnectionState.Offline)
                throw new ConnectionException(
                    "The existing connection must be closed before another can begin.");

            // Send -> Handshake
            Task.Run(() => {
                connectionManager.Connect(remoteEP);
            });
        }

        public void Send(string message)
        {
            if (Connection.ConnectionState != ConnectionState.Connected)
                throw new ConnectionException(
                    "Cannot send message without a valid connection.");

            connectionManager.Send(Protocol.Content_Message, message);
        }

        public string Receive()
        {
            if (Connection.ConnectionState != ConnectionState.Connected)
                throw new ConnectionException(
                    "Cannot send message without a valid connection.");

            // Receive() logic here
            return Connection.Receive();
        }

        public void Stop()
        {
            if (Connection.ConnectionState == ConnectionState.Offline)
                return;
            
            connectionManager.Stop(ConnectionLossType.Local_Disconnect);

            if (ctorPort.HasValue)
                Connection = new Connection(ctorPort.Value);
            else if (ctorEP != null)
                Connection = new Connection(ctorEP);
            else
                Connection = new Connection();

            connectionManager = new ConnectionManager(Connection);
        }

        public event MessageEvent MessageReceived
        {
            add
            {
                connectionManager.MessageReceived += value;
            }
            remove
            {
                connectionManager.MessageReceived -= value;
            }
        }
        public event ConnectionClosedEvent ConnectionClosed
        {
            add
            {
                connectionManager.ConnectionClosed += value;
            }
            remove
            {
                connectionManager.ConnectionClosed -= value;
            }
        }
        public event ConnectionIssuesEvent ConnectionIssues
        {
            add
            {
                connectionManager.ConnectionIssues += value;
            }
            remove
            {
                connectionManager.ConnectionIssues -= value;
            }
        }

        public Connection Connection { get; internal set; }
        #endregion
    }
}
