using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLib.Discover
{
    public class ServerItem
    {
        public string Name { get; private set; }
        public string Address { get; private set; }
        public string Ping { get; private set; }
        public ushort Port { get; private set; }

        public ServerItem(string name, string address, string ping, ushort port)
        {
            this.Name = name;
            this.Address = address;
            this.Ping = ping;
            this.Port = port;
        }
    }
}
