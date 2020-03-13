using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;      //required
using System.Net.Sockets;    //required
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace TCPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 9005);
            // we set our IP address as server's address, and we also set the port: 9005

            server.Start();  // this will start the server

            while (true)   //we wait for a connection
            {
                TcpClient client = server.AcceptTcpClient();  //if a connection exists, the server will accept it

                NetworkStream ns = client.GetStream(); //networkstream is used to send/receive messages


                while (client.Connected)  //while the client is connected, we look for incoming messages
                {
                    byte[] headerData = new byte[Header.RawHeader.Size];
                    ns.Read(headerData, 0, headerData.Length);
                    var header = new Header(headerData);
                    byte[] body = new byte[header.TcpPayloadLengthOrUdpSequenceNumber];     //the messages arrive as byte array
                    ns.Read(body, 0, body.Length);   //the same networkstream reads the message sent by the client
                    var str = Encoding.Default.GetString(body);

                    Console.WriteLine(str); //now , we write the message as string
                }
            }
        }
    }
}
