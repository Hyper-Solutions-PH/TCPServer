using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;      //required
using System.Net.Sockets;    //required
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TCPServer
{
    class Server
    {
        static Dictionary<Guid, Device> deviceDictionary = new Dictionary<Guid, Device>();
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
                    byte[] bodyData = new byte[header.TcpPayloadLengthOrUdpSequenceNumber];     //the messages arrive as byte array
                    ns.Read(bodyData, 0, bodyData.Length);   //the same networkstream reads the message sent by the client
                    if(header.PayloadType == PayloadType.Json)
                    {
                        var str = Encoding.Default.GetString(bodyData);
                        var parsedObject = JObject.Parse(str);
                        var operation = (string)parsedObject["OPERATION"];
                        if ( operation == "CONNECT" && parsedObject["PARAMETER"] != null)
                        {
                            var dsno = (string)parsedObject["PARAMETER"]["DSNO"];
                            var session = new Guid((string)parsedObject["SESSION"]); 
                            deviceDictionary.Add(session, new Device(dsno));
                            Respond(ns, session, header);
                        }
                        else if( operation == "KEEPALIVE")
                        {
                            //KEEPALIVE can respond by simply sending back frame it got
                            var response = new byte[headerData.Length + bodyData.Length];
                            headerData.CopyTo(response, 0);
                            bodyData.CopyTo(response, headerData.Length);
                            Respond(ns, response);
                        }
                    }
                    else if(header.PayloadType == PayloadType.Binary)
                    {
                        var gps = new GPS(bodyData);
                        Console.WriteLine(gps);
                    }
                    
                }
            }
        }

        static void Respond(NetworkStream ns, Guid guid, Header header)
        {
            var body = new ConnectBody(guid);
            var response = new ResponseFrame(header, body);
            Respond(ns, response.Serialize());  
        }

        static void Respond(NetworkStream ns, byte[] response)
        {
            ns.Write(response);
        }
    }
}
