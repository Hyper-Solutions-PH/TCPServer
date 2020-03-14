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
        protected Body(string operation) => this.operation = operation;
    }

    class ConnectBody : Body
    {
        [JsonProperty("RESPONSE")]
        readonly Dictionary<string, dynamic> response = new Dictionary<string, dynamic>
        {
            {"DEVTYPE", 1 },
            {"ERRORCODE", 0 },
            {"ERRORCAUSE", "" },
            {"PRO", "1.0.5" },
            {"MASKCMD", 5 },
            {"VCODE", "" },
            {"S0", "" }
        };
        public ConnectBody(Guid session) : base("CONNECT")
        {
            this.session = session.ToString();
        }
    }
}
