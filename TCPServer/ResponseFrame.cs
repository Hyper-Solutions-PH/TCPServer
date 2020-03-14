using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TCPServer
{
    class ResponseFrame
    {
        Header header;
        Body body;

        public ResponseFrame(Header header, Body body)
        {
            this.header = header.Clone();
            this.body = body;

        }
        public byte[] Serialize()
        {
            var json = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(body));
            header.TcpPayloadLengthOrUdpSequenceNumber = json.Length;
            var rawHeader = header.GetRawHeader();
            byte[] result = new byte[json.Length + rawHeader.Length];
            rawHeader.CopyTo(result, 0);
            json.CopyTo(result, rawHeader.Length);
            return result;
        }
    }
}
