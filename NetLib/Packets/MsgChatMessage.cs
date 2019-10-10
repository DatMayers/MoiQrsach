using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLib.Packets
{
    [Serializable]
    public class MsgChatMessage
    {
        public string Text { get; set; }
    }
}
