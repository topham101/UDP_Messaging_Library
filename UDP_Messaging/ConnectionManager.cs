using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UDP_Messaging
{
    public enum ConnectionState
    {
        Offline,
        Connecting,
        Pending_Connection,
        Connected
    }
    public enum ConnectionLossType
    {
        EndPoint_Disconnect,
        Local_Disconnect,
        Timeout,
    }
    public enum ConnectionIssue
    {
        SuspectedPacketLoss
    }

    /// <summary>
    /// Provides High(er)-Level functionality for the Connection Class.
    /// </summary>
    internal class ConnectionManager
    {
        private bool stopping = false;
        private const short connectTimeout = 2000;
        private const short listenerPollSpeed = 10;
        private const short pollSpeed = 10;
        private const short statusCheckFreqSpeed = 1000;
        private const short statusCheckTimeout = 8000;
        private readonly int timeout;
        private Connection connection;
        private Timer pollingService;
        private Timer connectionStatusService;
        private Stopwatch lastConnectionTestStopwatch;
        private PacketChecker packetChecker = new PacketChecker();

        internal CancellationTokenSource ConnectionToken { get; set; }

        public event MessageEvent MessageReceived;
        public event ConnectionClosedEvent ConnectionClosed;
        public event ConnectionIssuesEvent ConnectionIssues
        {
            add
            {
                packetChecker.ConnectionIssues += value;
            }
            remove
            {
                packetChecker.ConnectionIssues -= value;
            }
        }

        internal ConnectionManager(Connection connection, int _timeout = 400)
        {
            this.connection = connection;
            timeout = _timeout;
            ConnectionToken = new CancellationTokenSource();
            pollingService = new Timer(Poll, null, -1, pollSpeed);

            connection.ConnectionStatusChanged += RefreshPacketChecker;
        }

        internal void Listen()
        {
            connection.ConnectionState = ConnectionState.Pending_Connection;
            connection.Setup();
            
            Stopwatch sw = new Stopwatch();
            Tuple<byte, Protocol, string> message = null;
            var ep = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);

            while (!ConnectionToken.IsCancellationRequested)
            {
                sw.Start();
                if (connection.Available() > 0)
                {
                    string received = connection.Receive(ref ep);
                    message = ProtocolHandler.InterpretMessage(received);
                    if (message != null && packetChecker.CheckPacket(message.Item1) &&
                        message.Item2 == Protocol.Connection_Request)
                        break;
                }
                int timeLeft = listenerPollSpeed - sw.Elapsed.Milliseconds;
                if (timeLeft > 0)
                    Task.Delay(timeLeft).Wait();
                sw.Reset();
            }

            if (ConnectionToken.IsCancellationRequested)
                return;
            
            connection.Initialize(ep);
            Send(Protocol.Connection_Accepted);
            connection.ConnectionState = ConnectionState.Connected;
            PollServiceStart();
            ConnectionStatusServiceStart();
        }
        internal void Connect(IPEndPoint remoteEP)
        {
            connection.ConnectionState = ConnectionState.Connecting;
            connection.Setup();
            connection.Initialize(remoteEP);
            
            var pollStopwatch = new Stopwatch();
            var timeoutStopWatch = new Stopwatch();
            timeoutStopWatch.Start();
            Tuple<byte, Protocol, string> message = null;
            while (!ConnectionToken.IsCancellationRequested
                && timeoutStopWatch.ElapsedMilliseconds < connectTimeout)
            {
                pollStopwatch.Start();
                Send(Protocol.Connection_Request);
                if (connection.Available() > 0)
                {
                    string received = connection.Receive();
                    message = ProtocolHandler.InterpretMessage(received);
                    if (message != null || !packetChecker.CheckPacket(message.Item1))
                        break;
                }
                int timeLeft = listenerPollSpeed - pollStopwatch.Elapsed.Milliseconds;
                if (timeLeft > 0)
                    Task.Delay(timeLeft).Wait();
                pollStopwatch.Reset();
            }

            if (ConnectionToken.IsCancellationRequested)
                return;

            if (message != null && message.Item2 == Protocol.Connection_Accepted)
            {
                connection.ConnectionState = ConnectionState.Connected;
                PollServiceStart();
                ConnectionStatusServiceStart();
            }
            else
            {
                connection.UnInitialize();
                connection.ConnectionState = ConnectionState.Offline;
            }
        }

        internal void Stop(ConnectionLossType cause)
        {
            if (stopping)
                return;
            else stopping = true;

            ConnectionToken?.Cancel();
            lastConnectionTestStopwatch?.Stop();
            pollingService?.Change(-1, -1);
            connectionStatusService?.Change(-1, -1);
            if (connection.ConnectionState == ConnectionState.Connected)
            {
                Send(Protocol.Connection_Close);
                Send(Protocol.Connection_Close);
            }
            
            connection.UnInitialize();
            ConnectionClosed?.Invoke(cause);
            connection.ConnectionState = ConnectionState.Offline;
        }
        internal void Send(Protocol protocol, string message = "")
        {
            bool increment = true;
            if (protocol == Protocol.Connection_Test || protocol == Protocol.Connection_Test_Response)
                increment = false;

            connection.Send(ProtocolHandler.BuildMessage(packetChecker.GetPacketNumber(increment), protocol, message));
        }

        private Tuple<byte, Protocol, string> Receive()
        {
            var message = ProtocolHandler.InterpretMessage(connection.Receive());
            if (message.Item2 == Protocol.Connection_Test || message.Item2 == Protocol.Connection_Test_Response)
                return message;

            bool isValid = packetChecker.CheckPacket(message.Item1);
            
            return isValid ? message : null;
        }

        private void PollServiceStart()
        {
            pollingService.Change(0, pollSpeed);
        }
        private void Poll(object x)
        {
            while (connection.Available() > 0 && !ConnectionToken.IsCancellationRequested)
            {
                MessageHandler(Receive());
            }
        }
        private void MessageHandler(Tuple<byte, Protocol, string> message)
        {
            if (message == null)
                return;

            switch (message.Item2)
            {
                case Protocol.Connection_Test:
                    Send(Protocol.Connection_Test_Response);
                    break;
                case Protocol.Connection_Test_Response:
                    lastConnectionTestStopwatch.Restart();
                    break;
                case Protocol.Content_Message:
                    MessageReceived?.Invoke(message.Item3);
                    break;
                case Protocol.Connection_Close:
                    Stop(ConnectionLossType.EndPoint_Disconnect);
                    break;
                case Protocol.Connection_Request:
                case Protocol.Connection_Accepted:
                default:
                    break;
            }
        }

        private void ConnectionStatusServiceStart()
        {
            connectionStatusService = new Timer(
                ConnectionStatus, null, 0, statusCheckFreqSpeed);

            lastConnectionTestStopwatch = new Stopwatch();
            lastConnectionTestStopwatch.Start();
        }
        private void ConnectionStatus(object x)
        {
            Send(Protocol.Connection_Test);
            if (lastConnectionTestStopwatch.ElapsedMilliseconds > statusCheckTimeout)
            {
                Stop(ConnectionLossType.Timeout);
            }
        }
        private void RefreshPacketChecker(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Offline:
                    packetChecker = new PacketChecker();
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Pending_Connection:
                    packetChecker.Reset();
                    break;
                case ConnectionState.Connected:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
