using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Laba_5
{
    class Client
    {
        public TcpClient TcpClient { get; set; }
        public string Name { get; set; } = "";

        public string Buffer { get; set; } = "";

        public Client(TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;
        }

        public void BroadcastMessage(List<Client> clients, string message)
        {
            clients.ForEach(client =>
            {
                if(client.Name != this.Name)
                {
                    SendMessage(client, message);
                }
            });
        }

        public void SendMessage(string message)
        {
            SendMessage(this, message);
        }

        public static void SendMessage(Client client, string message)
        {
            byte[] byte_message = Encoding.GetEncoding(866).GetBytes(message);
            client.TcpClient.GetStream().Write(byte_message, 0, byte_message.Length);
        }

        public static bool SendIfClientExist(Client client, List<Client> clients, string message)
        {
            foreach(var cl in clients)
            {
                if(cl.Name == client.Name)
                {
                    cl.SendMessage(message);
                    return true;
                }
            }
            return false;
        }
        
        public static bool SendIfClientExist(string name, List<Client> clients, string message)
        {
            Client client = clients.FirstOrDefault(x => x.Name == name);
            if(client != null)
            {
                SendIfClientExist(client, clients, message);
            }
            return false;
        }
    }
}
