using System;
using System.Net;
using System.Net.Sockets;

namespace MultiServerBasic
{
    public class Client {
        public int ID; //ID assign to this client.
        public TCP Tcp; //Tcp connection.
        public UDP Udp;
        
        private Server _server; //Server where is this client.

        /// <summary>Initialise the client.</summary>
        /// <param name="ID">ID linked to the client.</param>
        /// <param name="server">Server linked to the client.</param>
        public Client(int ID,Server server) {
            this._server = server; //Set the server where is this client.
            this.ID = ID; //Set the id.
        }

        /// <summary>Connect the client with the tcp and udp protocol.</summary>
        /// <param name="tcpSocket">Socket to link client and server in tcp.(set to null if you don't want to change it)</param>
        /// <param name="udpEndpoint">IP of the client to send udp data.(set to null if you don't want to change it)</param>
        public void Connect(TcpClient tcpSocket, IPEndPoint udpEndpoint) {
            //Connect the tcp
            Tcp = new TCP(this);
            if (tcpSocket != null)
            {
                Tcp.Connect(tcpSocket);
            }

            //Connect the udp
            Udp = new UDP(this);
            if (udpEndpoint != null)
            {
                Udp.Connect(udpEndpoint);
            }
        }

        /// <summary>Disconnect the client (tcp and udp).</summary>
        private void Disconnect() {
            Tcp.Disconnect();
            Udp.Disconnect();
            _server.GetClients().Remove(ID); //Remove the client frome the server client list.
            _server.GetNetworkServerAction().RemovePlayer(ID);
        }
        
        public class TCP {
            private readonly Client _client; //Client of the tcp.
            private TcpClient _socket; //Socket that link the client to the server.
            private NetworkStream _stream; //The stream of tcp data.
            private Byte[] _receiveBuffer;

            /// <summary>Initialise the tcp protocol to be used.</summary>
            /// <param name="client">Client to be linked at the tcp protocol.</param>
            public TCP(Client client) {
                this._client = client; //Set the client.
                _receiveBuffer = new byte[4096]; //Set the max receive Buffer.
            }
            
            /// <summary>Disconnect the tcp protocol.</summary>
            public void Disconnect() {
                Console.WriteLine("Disconnecting client with ID : "+_client.ID);
                
                _socket.Close();
                _socket = null;
                _stream = null;
                _receiveBuffer = null;
            }

            /// <summary>Connect the tcp protocol.</summary>
            /// <param name="tcpSocket">tcp socket connected to the client.</param>
            public void Connect(TcpClient tcpSocket) {
                _socket = tcpSocket; //Set the socket (link between client and server).
                
                if (!_socket.Connected) {
                    Console.WriteLine("Error the connection as failed ! (unconnected after the connection request)");
                    return;
                }
                
                Console.WriteLine("Client ID : "+_client.ID+" successfully connected");

                _stream = _socket.GetStream(); //Get the tcp data stream.

                _stream.BeginRead(_receiveBuffer, 0, 4096, ReceiveCallback, null); //Listen incoming tcp data.
            }

            //Function call after receive data from client.
            private void ReceiveCallback(IAsyncResult result) {
                int packetLenght = _stream.EndRead(result); //Stop listen incoming tcp data.

                if (packetLenght <= 0) {
                    //I dont know what that mean but that seem to be call when the connection is interrupted between client and server.
                    Console.WriteLine("Error cause by the lenght of the packet <= 0 the client while be disconnected");
                    _client.Disconnect(); //Disconnect safely the client.
                    
                    return;
                }
                
                Byte[] data = new byte[packetLenght]; //Prepare to store the incoming data.
                Array.Copy(_receiveBuffer,data,packetLenght); //Free the receive buffer to be used by the listener.
                
                _stream.BeginRead(_receiveBuffer, 0, 4096, ReceiveCallback, null); //Restart listen incoming tcp data.

                
                //TCP receive data may contain multiple packets then handle all of them.
                Packet tempPacket = new Packet(data);

                while (tempPacket.GetUnreadLenght() > 4)
                {
                    Console.WriteLine("0 : "+tempPacket.ReadInt(false));
                    int tempLenght = tempPacket.ReadInt(true);
                    Console.WriteLine("1 : "+tempLenght);
                    Console.WriteLine("2 : "+tempPacket.GetUnreadLenght());
                    Console.WriteLine("3 : "+tempPacket.ReadInt(false));
                    _client.HandlePacket(new Packet(tempPacket.ReadBytes(tempLenght,true))); //Convert into a packet and handle it.
                }
                
                tempPacket.Dispose();
                
            }

            /// <summary>Send data from server to connection by the TCP protocol.</summary>
            /// <param name="packet">Packet to send.</param>
            public void SendPacket(Packet packet,bool reUsePacket)
            {
                int id = packet.ReadInt(false);
                if (id != 5)
                {
                    Console.WriteLine("sending TCP packet with id : "+id);
                }
                
                Packet packetToSend = new Packet(packet.ReadAllBytes());
                packetToSend.InsertInt(packet.GetLenght());
                //packet.InsertInt(packet.GetLenght());
                //If connection always exist.
                if (_socket != null) {
                    _stream.BeginWrite(packetToSend.ReadAllBytes(), 0, packetToSend.GetLenght(), null, null); //Send data to the client.
                }
                else {
                    Console.WriteLine("TCP client with client ID : "+_client.ID+" isn't connected, cannot send data");
                }
                packetToSend.Dispose();
                
                if(!reUsePacket) packet.Dispose();
            }
            
        }

        public class UDP
        {
            public IPEndPoint EndPoint; //Client IP to send UDP data
            private Client _client; //Client of the UDP
            private bool isUdpClientConnected;

            /// <summary>Initialise the UDP protocol to be used.</summary>
            /// <param name="client">Client to be linked at the UDP protocol.</param>
            public UDP(Client client)
            {
                _client = client; //Set the client
            }

            /// <summary>Connect the UDP protocol.</summary>
            /// <param name="endPoint">IP of the connection to send data.</param>
            public void Connect(IPEndPoint endPoint)
            {
                EndPoint = endPoint; //Set the IP to send UDP data
                isUdpClientConnected = true;
            }

            public void Disconnect()
            {
                EndPoint = null;
                isUdpClientConnected = false;
            }

            /// <summary>Send data from server to the connection by the UDP protocol.</summary>
            /// <param name="packet">Packet to send.</param>
            public void SendPacket(Packet packet,bool reUsePacket)
            {
                if (isUdpClientConnected)
                {
                    _client._server.SendUDPPacket(EndPoint,packet); //Ask the server tu send UDP data
                    Console.WriteLine($"sending udp data to {_client.ID} packet id : {packet.ReadInt(false)}");
                    if(!reUsePacket) packet.Dispose();
                }
                else
                {
                    Console.WriteLine("Attempting to send UDP data to an unconnected client (ID : "+_client.ID+")");
                }
            }
        }
        
        /// <summary>Handle data to be readied.</summary>
        /// <param name="packet">Packet to read.</param>
        public void HandlePacket(Packet packet) {
            //make sure the action will be executed in the right order.
            //_server.GetThreadManager().ExecuteOnMainThread(() => {
                    
                int packetID = packet.ReadInt(true); //Read the packet ID.
                _server.PacketHandlers[packetID](packet,ID); //Execute the function assign to this packet ID.
                    
            //});
        }
        
    }
}