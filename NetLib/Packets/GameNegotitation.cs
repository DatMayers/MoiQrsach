using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLib.Packets
{
    [Serializable]
    public class MsgSetReadyForGame
    {
        public bool IsReady { get; set; }
        public string Password { get; set; }
    }

    [Serializable]
    public class MsgBeginGame
    {
        public Guid OpponentId { get; set; }
        public string Password { get; set; }
    }

    [Serializable]
    public class MsgNewGame
    {
        public Guid Id { get; set; }
        public Guid PlayerA { get; set; }
        public Guid PlayerB { get; set; }
    }

    [Serializable]
    public class MsgUpdateGame
    {
        public Guid Id { get; set; }
        public int ScoresA { get; set; }
        public int ScoresB { get; set; }
    }

    [Serializable]
    public class MsgRemoveGame
    {
        public Guid Id { get; set; }
    }

}
