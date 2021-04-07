using System;

namespace MultiServerBasic
{
    public class NetworkServerAction
    {
        private readonly Server _server; //Server linked

        /// <summary>Initialise the server actions.</summary>
        /// <param name="server">Server to link.</param>
        public NetworkServerAction(Server server)
        {
            _server = server;
        }

        #region SendAction

        /// <summary>Send a packet to to attribute the client id.</summary>
        /// <param name="id">ID of the client to send this message.</param>
        public void SendConnected(int assignedID)
        {
            Packet connectedPacket = new Packet((int) Packet.ServerPacketIDReference.Connected);
            connectedPacket.Write(assignedID);
            
            _server.GetClients()[assignedID].Tcp.SendPacket(connectedPacket,false); //send "connected" Packet to assign an id

            Console.WriteLine("Client connection confirmation sent to client ID : "+assignedID);
        }

        public void NewPlayer(int playerClientID)
        {
            Packet NewPlayer = new Packet((int) Packet.ServerPacketIDReference.NewPlayer);
            NewPlayer.Write(playerClientID);
            _server.SendTCPMessageToAllConnections(NewPlayer,playerClientID);
        }
        
        public void UpdatePosOfAPlayer(float posX, float posY, float posZ, int playerClientID)
        {
            Packet UpdatePos = new Packet((int) Packet.ServerPacketIDReference.UpdatePosOfAPlayer);
            UpdatePos.Write(posX);
            UpdatePos.Write(posY);
            UpdatePos.Write(posZ);
            UpdatePos.Write(playerClientID);
            _server.SendUDPPacketToAllConnections(UpdatePos,playerClientID);
        }

        public void RemovePlayer(int playerClientID)
        {
            Packet RemovePlayer = new Packet((int) Packet.ServerPacketIDReference.RemovePlayer);
            RemovePlayer.Write(playerClientID);
            _server.SendTCPMessageToAllConnections(RemovePlayer,playerClientID);
        }

        #endregion


        #region ReceiveAction

        /// <summary>Action to do when a client ask to resend a message.</summary>
        /// <param name="packet">Packet receive.</param>
        /// <param name="clientID">Client ID of the sender</param>
        public void ResendMessage(Packet packet,int clientID)
        {
            Packet debugMessage = new Packet((int) Packet.ServerPacketIDReference.DebugMessage);
            debugMessage.Write("you ask to resend this message : "+packet.ReadString(true));

            _server.GetClients()[clientID].Tcp.SendPacket(debugMessage,false);
            
            packet.Dispose(); //this is the last function that use this packet so she need to dispose it
        }

        public void UpdatePos(Packet packet, int clientID)
        {
            float posX = packet.ReadFloat(true);
            float posY = packet.ReadFloat(true);
            float posZ = packet.ReadFloat(true);
            UpdatePosOfAPlayer(posX,posY,posZ,clientID);
        }

        #endregion

    }
}