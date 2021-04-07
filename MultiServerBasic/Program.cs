using System;

namespace MultiServerBasic
{
    internal class Program
    {
        

        public static void Main(string[] args)
        {
            var server = new Server(10,50,28707);
            String t = Console.ReadLine();
            if (t.Contains("test"))
            {
                Packet packet = new Packet((int) Packet.ServerPacketIDReference.NewPlayer);
                packet.Write(0);
                server.GetClients()[1].Tcp.SendPacket(packet,false);
            }
        }
    }
}