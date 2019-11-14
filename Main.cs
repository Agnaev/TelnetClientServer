using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Laba_5
{
    public static class MainMain
    {
        public static void Main(string[] args)
        {
            Run();
        }

        private static int Port
        {
            get
            {
                return 7777;
            }
        }

        private static string IpAddres
        {
            get
            {
                return "192.168.1.4";
            }
        }

        public static void Run()
        {
            int bufferSize = 1;
            byte[] buffer = new byte[bufferSize];

            TcpListener Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IpAddres), Port));
            NetworkStream stream;
            List<Client> Clients = new List<Client>();

            string help = "\r\nuse \"\\\" for sending private message\r\n" +
                "use \"\\l\" list view\r\n" +
                "use \"\\e\" for exit" +
                "use \"\\h\" for get help.\r\n";

            int skip = 0; //wtf??

            Listener.Start();

            while (true)
            {
                if (Listener.Pending())
                {
                    TcpClient newClient = Listener.AcceptTcpClient();
                    stream = newClient.GetStream();
                    byte[] message = Encoding.GetEncoding(866).GetBytes("Enter your name: ");
                    stream.Write(message, 0, message.Length);
                    Clients.Add(new Client(newClient));
                }
                else
                {
                    Clients.ForEach(delegate(Client client)
                    {
                        if (client.TcpClient.Connected)
                        {
                            int count;
                            stream = client.TcpClient.GetStream();
                            while (stream.DataAvailable)
                            {
                                count = client.TcpClient.GetStream().Read(buffer, 0, bufferSize);
                                if (buffer[0] == 27)
                                {
                                    skip = 3;
                                }

                                if (skip == 0)
                                {
                                    client.Buffer += Encoding.GetEncoding(866).GetString(buffer, 0, count);
                                }
                                else
                                {
                                    skip--;
                                }

                                while (client.Buffer.IndexOf('\b') > -1)
                                {
                                    client.Buffer = client.Buffer.Remove(client.Buffer.IndexOf('\b'), 1);
                                    if (client.Buffer.Length != 0)
                                    {
                                        client.Buffer = client.Buffer.Remove(client.Buffer.Length - 1);
                                    }
                                }
                            }


                            if (client.Buffer.Length != 0 && client.Buffer[client.Buffer.Length - 1] == '\n')
                            {
                                if (client.Name.Length < 3)
                                {
                                    client.Name = client.Buffer.Remove(client.Buffer.Length - 2);

                                    if (client.Name.Length < 3)
                                    {
                                        client.SendMessage("Name must contain at least 3 characters. Enter a new name.\r\n");
                                    }
                                    else if (Clients.Where(x => x != client && x.Name == client.Name).ToList().Count != 0)
                                    {
                                        client.SendMessage("The selected name is already taken. Enter a new name.\r\n");
                                        client.Name = "";
                                    }
                                    else if (client.Name.Length > 2)
                                    {
                                        string message = $"{client.Name} - connected\r\n";
                                        Console.WriteLine(message);
                                        client.BroadcastMessage(Clients, message);
                                        client.SendMessage(help);
                                    }
                                }
                                else if (client.Buffer != "\r\n")
                                {
                                    Console.WriteLine($"{client.Name}: {client.Buffer}");
                                    if (client.Buffer[0] == '\\')
                                    {
                                        if (client.Buffer.Length == 4)
                                        {
                                            if (client.Buffer[1] == 'h')
                                            {
                                                client.SendMessage(help);
                                            }
                                            else if (client.Buffer[1] == 'l')
                                            {
                                                string info = "";

                                                Clients.ForEach(cl =>
                                                {
                                                    info += $"{cl.Name}\r\n";
                                                });

                                                client.SendMessage(info);
                                            }
                                            else if (client.Buffer[1] == 'e')
                                            {
                                                client.BroadcastMessage(Clients, $"{client.Name} - Disconnected\r\n");
                                                client.TcpClient.Close();
                                            }
                                            else
                                            {
                                                client.SendMessage("Wrong command.\r\n");
                                            }
                                        }
                                        else if (client.Buffer.IndexOf(' ') != -1)
                                        {
                                            string name = client.Buffer.Substring(1);
                                            name = name.Remove(name.IndexOf(' '));

                                            if (name.Length < 3)
                                            {
                                                client.SendMessage("Receiver name is incorrect. Try again.\r\n");
                                            }
                                            else
                                            {
                                                string buff = client.Buffer.Substring(name.Length + 2);
                                                if (Client.SendIfClientExist(name, Clients, $"{client.Name}: {buff}"))
                                                {
                                                    client.SendMessage("Client with this name does not exist.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            client.SendMessage("Wrong command\r\n");
                                        }
                                    }
                                    else
                                    {
                                        client.BroadcastMessage(Clients, $"{client.Name}: {client.Buffer}");
                                    }
                                }
                                if (client.TcpClient.Connected)
                                {
                                    client.Buffer = string.Empty;
                                }
                                else
                                {
                                    Clients.Remove(client);
                                }
                            }
                        }
                        else
                        {
                            client.TcpClient.Close();
                            Clients.Remove(client);
                        }
                        Thread.Sleep(1);
                    });
                }
            }
        }
    }
}
