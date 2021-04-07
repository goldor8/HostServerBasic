using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MultiServerBasic {
    
    public class Server {
        
        private int _maxConnection; //The max connection.
        private int _updatePerSecond; //The update frequency.
        private int _millisecondPerUpdate; //Convert the update frequency in millisecond per update.
        private bool _running = true; //If the server is running.
        
        private readonly int _port; //The port currently listening.
        private readonly UdpClient _udpListener;
        private readonly TcpListener _tcpListener; //The listener of tcp connection.
        private readonly NetworkServerAction _networkServerAction; //The class to do main server actions.
        private readonly ThreadManager _mainThreadManager; //The manager of the main thread.
        private readonly Dictionary<int ,Client> _clients; //The list of client with an id assigned.
        
        public delegate void ActionHandler(Packet packet,int clientID); //Delegate an packet action.
        public Dictionary<int, ActionHandler> PacketHandlers; //Assign an packet ID to an action.

        /// <summary>Start a new Server.</summary>
        /// <param name="maxConnection">Max connection to this server.</param>
        /// <param name="updateFrequency">Update frequency to reach.</param>
        /// <param name="port">Port listening the server?</param>
        public Server(int maxConnection ,int updateFrequency ,int port) {
            //Set the update frequency.
            _updatePerSecond = updateFrequency;
            _millisecondPerUpdate = 1000 / updateFrequency;
            
            
            _maxConnection = maxConnection; //Set the max connections.
            _port = port; //Set the port.
            
            _clients = new Dictionary<int, Client>(); //Initialise the new list of client with id assign to it.
            _networkServerAction = new NetworkServerAction(this); //Initialise main server actions.
            _mainThreadManager = new ThreadManager(); //Initialise the mainThreadManager.
            
            InitialisePacketsIDReader(); //Link actions to an packet ID.

            Thread mainThread = new Thread(MainUpdateThread); //Prepare a new thread to execute the loop.
            mainThread.Start();

            _tcpListener = new TcpListener(IPAddress.Any, _port); //Start the listener to accept new tcp connection.
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(NewTCPConnection,null);
            
            _udpListener = new UdpClient(_port);
            _udpListener.BeginReceive(UDPReceiveCallback, null);
        }

        //Function call when a tcp client attempt a connection to the server.
        private void NewTCPConnection(IAsyncResult result) {
            TcpClient tcpClient = _tcpListener.EndAcceptTcpClient(result);//End the listener.
            Console.WriteLine("New client connection with ip "+tcpClient.Client.RemoteEndPoint);
            _tcpListener.BeginAcceptTcpClient(NewTCPConnection,null); //Restart the listener to accept new tcp connection.
            
            //Check if a place is available for this new connection.
            for (int i = 0; i < _maxConnection; i++) {
                if (!_clients.ContainsKey(i)) {
                    
                    _clients.Add(i,new Client(i,this)); //Assign the ID to the client.
                    Console.WriteLine("Client "+tcpClient.Client.RemoteEndPoint+" have been assign to client ID : "+i);
                    _clients[i].Connect(tcpClient,null); //Initialise the sockets.
                    _networkServerAction.SendConnected(i); //Send the ID to client.
                    _networkServerAction.NewPlayer(i);
                    
                    //send new player packet for all player already connected
                    for (int x = 0; x < _maxConnection; x++)
                    {
                        if (x != i && _clients.ContainsKey(x))
                        {
                            Packet packet = new Packet((int) Packet.ServerPacketIDReference.NewPlayer);
                            packet.Write(x);
                            _clients[i].Tcp.SendPacket(packet,false);
                            Console.WriteLine("sending player "+x+" to "+i);
                        }
                    }
                    
                    return;
                }
            }
            
            //If there is no more place.
            Console.WriteLine("The max connection have been reach("+_maxConnection+") refusing client "+tcpClient.Client.RemoteEndPoint);
            //todo: send a packet to explain the client there is no more place available
        }

        private void UDPReceiveCallback(IAsyncResult result)
        {
            try
            {
                IPEndPoint senderIP = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveData = _udpListener.EndReceive(result,ref senderIP);
                _udpListener.BeginReceive(UDPReceiveCallback, null);
                Packet packet = new Packet(receiveData);
                int fromClient = packet.ReadInt(true);
                for (int i = 0; i < _maxConnection; i++)
                {
                    if (_clients.ContainsKey(i))
                    {
                        if (_clients[i].Udp.EndPoint == null)
                        {
                            _clients[i].Udp.Connect(senderIP);
                        }
                        if (_clients[i].ID == fromClient)
                        {
                            _clients[i].HandlePacket(packet);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SendUDPPacket(IPEndPoint endPoint, Packet packet)
        {
            _udpListener.BeginSend(packet.ReadAllBytes(), packet.GetLenght(), endPoint, null, null);
        }

        public void SendUDPPacketToAllConnections(Packet packet)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                _clients[i].Udp.SendPacket(packet,true);
            }
            packet.Dispose();
        }
        
        public void SendUDPPacketToAllConnections(Packet packet,int exceptedConnectionID)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                if (exceptedConnectionID != _clients[i].ID)
                {
                    _clients[i].Udp.SendPacket(packet,true);
                }
            }
            packet.Dispose();
        }
        
        public void SendUDPPacketToAllConnections(Packet packet,int[] exceptedConnectionsID)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                if (!exceptedConnectionsID.Contains(_clients[i].ID))
                {
                    _clients[i].Udp.SendPacket(packet,true);
                }
            }
            packet.Dispose();
        }

        /// <summary>Send a packet to all connection with the tcp protocol.</summary>
        /// <param name="packet">Packet to send.</param>
        public void SendTCPMessageToAllConnections(Packet packet)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                _clients[i].Tcp.SendPacket(packet,true);
            }
            packet.Dispose();
        }
        
        public void SendTCPMessageToAllConnections(Packet packet,int exceptedConnectionID)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                if (exceptedConnectionID != _clients[i].ID)
                {
                    _clients[i].Tcp.SendPacket(packet,true);
                }
            }
            packet.Dispose();
        }
        
        public void SendTCPMessageToAllConnections(Packet packet,int[] exceptedConnectionsID)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                if (!exceptedConnectionsID.Contains(_clients[i].ID))
                {
                    _clients[i].Tcp.SendPacket(packet,true);
                }
            }
            packet.Dispose();
        }
        
        /// <summary>Set the max connection to the server.</summary>
        /// <param name="max">Max connection to the server.</param>
        public void SetMaxConnection(int max) {
            _maxConnection = max;
        }

        /// <summary>Get the max connection number of the server.</summary>
        /// <returns>Return the connection number of the server.</returns>
        public int GetMaxConnection() {
            return _maxConnection;
        }

        /// <summary>Set the update frequency to reach.</summary>
        /// <param name="frequency">The update frequency.</param>
        public void SetUpdateFrequency(int frequency) {
            _updatePerSecond = frequency;
            _millisecondPerUpdate = 1000 / frequency;
        }

        /// <summary>Get the update frequency.</summary>
        /// <returns>Return the update frequency.</returns>
        public int GetUpdateFrequency() {
            return _updatePerSecond;
        }

        /// <summary>Get the client list if the ID assign to it.</summary>
        /// <returns>Return the list of clients with their ID.</returns>
        public Dictionary<int, Client> GetClients() {
            return _clients;
        }

        /// <summary>Get the main thread manager.</summary>
        /// <returns>Return the main thread manager.</returns>
        public ThreadManager GetThreadManager() {
            return _mainThreadManager;
        }

        public NetworkServerAction GetNetworkServerAction()
        {
            return _networkServerAction;
        }

        /// <summary>Set the server running state.</summary>
        /// <param name="isRunning">Server running state.</param>
        public void SetIsRunning(bool isRunning)
        {
            _running = isRunning;
        }

        /// <summary>see if the server is running.</summary>
        /// <returns>return the server state.</returns>
        public bool IsRunning() {
            return _running;
        }

        //Loop update of the main thread
        private void MainUpdateThread() {
            DateTime nextLoop = DateTime.Now;
            while (_running) {
                
                if (nextLoop < DateTime.Now) {
                    nextLoop = nextLoop.AddMilliseconds(_millisecondPerUpdate);
                    _mainThreadManager.Update();
                }
                if(nextLoop > DateTime.Now) {
                    Thread.Sleep(nextLoop - DateTime.Now);
                }
                
            }
        }
        
        //Link actions to an packet ID
        private void InitialisePacketsIDReader() {
            PacketHandlers = new Dictionary<int, ActionHandler>() {
                {(int) Packet.ClientPacketIDReference.ResendMessage, _networkServerAction.ResendMessage},
                {(int) Packet.ClientPacketIDReference.UpdatePos, _networkServerAction.UpdatePos}
            };
        }

    }
}