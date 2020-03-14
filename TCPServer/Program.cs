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
    class Program
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
                    byte[] body = new byte[header.TcpPayloadLengthOrUdpSequenceNumber];     //the messages arrive as byte array
                    ns.Read(body, 0, body.Length);   //the same networkstream reads the message sent by the client
                    var str = Encoding.Default.GetString(body);
                    if(header.PayloadType == PayloadType.Json)
                    {
                        var parsedObject = JObject.Parse(str);
                        var operation = (string)parsedObject["OPERATION"];
                        if ( operation == "CONNECT" && parsedObject["PARAMETER"] != null)
                        {
                            var dsno = (string)parsedObject["PARAMETER"]["DSNO"];
                            var session = new Guid((string)parsedObject["SESSION"]); 
                            deviceDictionary.Add(session, new Device(dsno));
                            Respond(ns, session, header);
                        }
                    }
                    Console.WriteLine(str); //now , we write the message as string
                }
            }
        }

        static void Respond(NetworkStream ns, Guid guid, Header header)
        {
            var body = new ConnectBody(guid);
            var response = new ResponseFrame(header, body);
            ns.Write(response.Serialize());    
        }
    }
}
