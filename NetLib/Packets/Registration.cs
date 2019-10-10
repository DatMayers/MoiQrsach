using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLib.Packets
{
    [Serializable]
    public class MsgRegisterName
    {
        public string ClientName { get; set; }
    }

    [Serializable]
    public class MsgSessionId
    {
        public Guid Id { get; set; }
    }
}
