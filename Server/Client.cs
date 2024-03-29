﻿using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Client
    {
        private Socket _socket;
        private NetworkStream _stream;

        public event Action<Client,Message> MessageRecived;

        public int Name { get; private set; }

        public NetworkStatus Status 
        { 
            get 
            {
                if (_socket == null) 
                {
                    return NetworkStatus.Offline;
                }

                if ((_socket.Poll(1000, SelectMode.SelectRead) && (_socket.Available == 0)))
                {
                    return NetworkStatus.Offline;
                }

                else
                {
                   return NetworkStatus.Online;
                }
            }
        }

        public Client(Socket socket)
        {
            _socket = socket;
            _stream = new NetworkStream(_socket);
            Name = socket.GetHashCode();

            //запускаем таску приема, асинхронно чтоб не подвешивал UI сервера
            Task.Run(() => StartReceive());
        }

        private void StartReceive()
        {
            while (true)
            {
                Message message = new Message();

                try
                {
                    //буфер для размера сообщения
                    byte[] bufferSize = new byte[2];
                    //читаем входящий поток
                    int readBytes = _stream.Read(bufferSize, 0, 2);
                    //если посылка пуста - выходим
                    if (readBytes == 0)
                        break;
                    //запоминаем сколько весит сообщение
                    message.Size = BitConverter.ToInt16(bufferSize, 0);

                    //переопределяем буфер для сообщения
                    bufferSize = new byte[message.Size];
                    //считываем поток в буфер с учетом размера
                    int readMessage = _stream.Read(bufferSize, 0, message.Size);
                    //если посылка пуста - выходим
                    if (readBytes == 0)
                        break;
                    //приводим массив полученых байтов к человеческому виду :)
                    message.Text = Encoding.UTF8.GetString(bufferSize);
                    message.Login = Name;

                    //уведомляем подписчиков о приеме нового сообщения от клиента
       
                    MessageRecived.Invoke(this, message);
                }
                catch (Exception ex) 
                {
                    LogManager.AddLog(ex.Message,MessageStatus.Warning);
                    return;
                }

        }
        }

        /// <summary>
        /// посылает сообщение текущему клиенту (текущему экземпляру)
        /// </summary>
        /// <param name="message"></param>
        internal void SendMessageToClient(Message message)
        { 
                if (_socket != null)
                {
                    //посылаем сообщение (размер, логин, и текст)
                    _socket.Send(BitConverter.GetBytes((Int16)message.Size), 2, SocketFlags.None);
                    _socket.Send(BitConverter.GetBytes((Int32)message.Login), 4, SocketFlags.None);
                    _socket.Send(Encoding.UTF8.GetBytes(message.Text));
                }
        }
    }
}
