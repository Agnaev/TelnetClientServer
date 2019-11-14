using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Laba_5
{
    class Program
    {
        static void Main_(string[] args)
        {
            int BufferSize = 1;                  // Размер буффера чтения
            byte[] buffer = new byte[BufferSize];   // Устанавливаем размер буфера для чтения запроса

            int ipPort = 7777;
            //string ipAddress = "127.0.0.1";
            string ipAddress = "192.168.1.4";

            IPAddress ip = IPAddress.Parse(ipAddress);
            IPEndPoint ipLocalEndPoint = new IPEndPoint(ip, ipPort);

            NetworkStream stream;                   // Поток для чтения/передачи данных с сокета
            TcpListener Listener = new TcpListener(ipLocalEndPoint);
            List<Client> MyClients = new List<Client>();

            string help = "\r\nДля отправки лчного сообщения, введите символ \"\\\", затем имя получателя, пробел и ваше сообщение.\r\n";
            help += "Для вывода списка подключённых клиентов, введите \"\\i\".\r\nДля вывода подсказки, введите \"\\h\".\r\n";
            help += "Для выхода из сети, введите \"\\q\".\r\n\r\n";

            int skip = 0;

            Listener.Start();
            while (true)
            {
                if (Listener.Pending())
                {
                    TcpClient tc = Listener.AcceptTcpClient();
                    stream = tc.GetStream();
                    byte[] mess = Encoding.GetEncoding(866).GetBytes("Введите своё имя: ");
                    stream.Write(mess, 0, mess.Length);
                    MyClients.Add(new Client(tc));
                }
                else
                {
                    for (int i = MyClients.Count - 1; i >= 0; i--)
                    {
                        if (MyClients[i].TcpClient.Connected)
                        {
                            int count;
                            stream = MyClients[i].TcpClient.GetStream();
                            while (stream.DataAvailable)
                            {
                                count = MyClients[i].TcpClient.GetStream().Read(buffer, 0, BufferSize);
                                if (buffer[0] == 27)
                                {
                                    skip = 3;
                                }

                                if (skip == 0)
                                {
                                    MyClients[i].Buffer += Encoding.GetEncoding(866).GetString(buffer, 0, count);
                                }
                                else
                                {
                                    skip--;
                                }

                                while (MyClients[i].Buffer.IndexOf('\b') > -1)
                                {
                                    MyClients[i].Buffer = MyClients[i].Buffer.Remove(MyClients[i].Buffer.IndexOf('\b'), 1);
                                    if (MyClients[i].Buffer.Length != 0)
                                    {
                                        MyClients[i].Buffer = MyClients[i].Buffer.Remove(MyClients[i].Buffer.Length - 1);
                                    }
                                }
                            }

                            if (MyClients[i].Buffer.Length != 0 && MyClients[i].Buffer[MyClients[i].Buffer.Length - 1] == '\n')
                            {
                                if (MyClients[i].Name.Length < 3)
                                {
                                    MyClients[i].Name = MyClients[i].Buffer.Remove(MyClients[i].Buffer.Length - 2);

                                    if (MyClients[i].Name.Length < 3)
                                    {
                                        byte[] mess = Encoding.GetEncoding(866).GetBytes("Имя должно содержать минимум 3 символа. Введите новое имя.\r\n");
                                        stream.Write(mess, 0, mess.Length);
                                    }
                                    else
                                    {
                                        for (int j = 0; j < MyClients.Count; j++)
                                        {
                                            if (i != j && MyClients[i].Name == MyClients[j].Name && MyClients[j].TcpClient.Connected)
                                            {
                                                byte[] mess = Encoding.GetEncoding(866).GetBytes("Выбранное имя уже занято. Введите новое имя.\r\n");
                                                stream.Write(mess, 0, mess.Length);
                                                MyClients[i].Name = "";
                                                break;
                                            }
                                        }

                                        if (MyClients[i].Name.Length > 2)
                                        {
                                            byte[] mess = Encoding.GetEncoding(866).GetBytes(MyClients[i].Name + " подключился\r\n");
                                            Console.WriteLine(MyClients[i].Name + " подключился.");
                                            for (int j = MyClients.Count - 1; j >= 0; j--)
                                            {
                                                if (i != j && MyClients[j].Name != "")
                                                {
                                                    MyClients[j].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                                }
                                            }

                                            mess = Encoding.GetEncoding(866).GetBytes(help);
                                            MyClients[i].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                        }
                                    }
                                }
                                else if(MyClients[i].Buffer != "\r\n")
                                {
                                    Console.Write(MyClients[i].Name + ": " + MyClients[i].Buffer);

                                    if (MyClients[i].Buffer[0] == '\\')
                                    {
                                        if (MyClients[i].Buffer.Length == 4)
                                        {
                                            if (MyClients[i].Buffer[1] == 'h')
                                            {
                                                byte[] mess = Encoding.GetEncoding(866).GetBytes(help);
                                                MyClients[i].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                            }
                                            else if (MyClients[i].Buffer[1] == 'i')
                                            {
                                                string info = "";
                                                foreach (Client c in MyClients)
                                                {
                                                    info += c.Name + "\r\n";
                                                }

                                                byte[] mess = Encoding.GetEncoding(866).GetBytes(info);
                                                MyClients[i].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                            }
                                            else if (MyClients[i].Buffer[1] == 'q')
                                            {
                                                MyClients[i].TcpClient.Close();
                                            }
                                            else
                                            {
                                                byte[] mess = Encoding.GetEncoding(866).GetBytes("Неверная команда.\r\n");
                                                MyClients[i].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                            }
                                        }
                                        else if (MyClients[i].Buffer.IndexOf(' ') != -1)
                                        {
                                            string name = MyClients[i].Buffer.Substring(1);
                                            name = name.Remove(name.IndexOf(' '));

                                            if (name.Length < 3)
                                            {
                                                byte[] mess = Encoding.GetEncoding(866).GetBytes("Имя получателя введено неверно. Попробуйте снова.\r\n");
                                                MyClients[i].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                            }
                                            else
                                            {
                                                bool exists = false;
                                                for (int j = MyClients.Count - 1; j >= 0; j--)
                                                {
                                                    if (MyClients[j].Name == name)
                                                    {
                                                        string buff = MyClients[i].Buffer.Substring(name.Length + 2);
                                                        byte[] mess = Encoding.GetEncoding(866).GetBytes(MyClients[i].Name + ": " + buff);
                                                        MyClients[j].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                                        exists = true;
                                                        break;
                                                    }
                                                }

                                                if (!exists)
                                                {
                                                    byte[] mess = Encoding.GetEncoding(866).GetBytes("Клиент с таким именем не существует.\r\n");
                                                    MyClients[i].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            byte[] mess = Encoding.GetEncoding(866).GetBytes("Неверная команда.\r\n");
                                            MyClients[i].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                        }
                                    }
                                    else
                                    {
                                        byte[] mess = Encoding.GetEncoding(866).GetBytes(MyClients[i].Name + ": " + MyClients[i].Buffer);
                                        for (int j = MyClients.Count - 1; j >= 0; j--)
                                        {
                                            if (i != j && MyClients[j].Name != "" && MyClients[j].TcpClient.Connected)
                                            {
                                                MyClients[j].TcpClient.GetStream().Write(mess, 0, mess.Length);
                                            }
                                        }
                                    }
                                }
                                if (MyClients[i].TcpClient.Connected)
                                {
                                    MyClients[i].Buffer = "";
                                }
                                else
                                {
                                    MyClients.RemoveAt(i);
                                }
                            }
                        }
                        else
                        {
                            MyClients[i].TcpClient.Close();
                            MyClients.RemoveAt(i);
                        }
                        Thread.Sleep(1);
                    }
                }
            }
        }
    }
}