using System;
using System.Text;
using System.Net;      
using System.Net.Sockets;    
using Newtonsoft.Json.Linq;

namespace TCPServer
{
    class Server
    {
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 9005);
            // we set our IP address as server's address, and we also set the port: 9005

            server.Start();  // this will start the server

            while (true)   //we wait for a connection
            {
                server.BeginAcceptTcpClient(new AsyncCallback(HandleClientCallback), server);  //if a connection exists, the server will accept it
            }
        }

        static void HandleClientCallback(IAsyncResult ar)
        {
            TcpListener server = (TcpListener)ar.AsyncState;
            TcpClient client = server.EndAcceptTcpClient(ar);
            NetworkStream ns = client.GetStream(); //networkstream is used to send/receive messages

            while (client.Connected)  //while the client is connected, we look for incoming messages
            {
                var header = ReadIncomingHeader(ns);

                if (header.PayloadType == PayloadType.Json)
                {
                    HandleJsonRequest(ns, header);
                }
                else if (header.PayloadType == PayloadType.Binary)
                {
                    InterpretGps(ns, header);
                }
            }
        }

        static Header ReadIncomingHeader(NetworkStream ns)
        {
            byte[] headerData = new byte[Header.RawHeader.Size];
            ns.Read(headerData, 0, headerData.Length);
            return new Header(headerData);
        }

        static JObject ReadJsonBody(byte[] bodyData)
        {
            var str = Encoding.Default.GetString(bodyData);
            return JObject.Parse(str);
        }

        static void HandleJsonRequest(NetworkStream ns, Header header)
        {
            byte[] bodyData = GetIncomingBody(ns, header);
            var jsonBody = ReadJsonBody(bodyData);
            try
            {
                var operation = (OperationType)Enum.Parse(typeof(OperationType), (string)jsonBody["OPERATION"]);
                var session = new Guid((string)jsonBody["SESSION"]);

                if (operation == OperationType.CONNECT)
                {
                    Respond<ConnectBody>(ns, session, header);
                }
                else if (operation == OperationType.KEEPALIVE)
                {
                    Respond<KeepAliveBody>(ns, session, header);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Invalid operation! {e.Message}");
            }
        }

        static void InterpretGps(NetworkStream ns, Header header)
        {
            var bodyData = GetIncomingBody(ns, header);
            var gps = new GPS(bodyData);
            Console.WriteLine(gps);
        }

        static byte[] GetIncomingBody(NetworkStream ns, Header header)
        {
            byte[] bodyData = new byte[header.TcpPayloadLengthOrUdpSequenceNumber];
            ns.Read(bodyData, 0, bodyData.Length);
            return bodyData;
        }
        static void Respond<BodyType>(NetworkStream ns, Guid guid, Header header) where BodyType : Body
        {
            var body = (BodyType)Activator.CreateInstance(typeof(BodyType), new object[] { guid });
            var response = new ResponseFrame(header, body);
            ns.Write(response.Serialize());
        }

        enum OperationType { UNSUPPORTED = 0, CONNECT, KEEPALIVE }
    }
}
