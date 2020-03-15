using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TCPServer
{
    abstract class Body
    {
        [JsonProperty("MODULE")]
        readonly public string module = "CERTIFICATE";
        [JsonProperty("OPERATION")]
        readonly public string operation;
        [JsonProperty("SESSION")]
        public string session { get; set; }
        protected Body(string operation, Guid session)
        {
            this.operation = operation;
            this.session = session.ToString();
        }

    }

    class ConnectBody : Body
    {
        [JsonProperty("RESPONSE")]
        readonly Dictionary<string, dynamic> response = new Dictionary<string, dynamic>
        {
            //Doc does not specify how these fields should be filled so I simply took the example
            {"DEVTYPE", 1 },
            {"ERRORCODE", 0 },
            {"ERRORCAUSE", "" },
            {"PRO", "1.0.5" },
            {"MASKCMD", 5 },
            {"VCODE", "" },
            {"S0", "" }
        };
        public ConnectBody(Guid session) : base("CONNECT", session) { }

    }

    class KeepAliveBody : Body
    {
        public KeepAliveBody(Guid session) : base("KEEPALIVE", session) { }
    }
}
