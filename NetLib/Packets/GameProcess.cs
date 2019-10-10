using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLib.Packets
{
    [Serializable]
    public class MsgPlace
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Len { get; set; }
        public bool Vertical { get; set; }
    }

    [Serializable]
    public class MsgTurn
    {
    }

    [Serializable]
    public class MsgFire
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool Dead { get; set; }
    }

    [Serializable]
    public class MsgFireResult
    {
        public bool Miss { get; set; }
        public bool Dead { get; set; }
    }

    [Serializable]
    public enum GameState
    {
        Init,
        TurnA,
        TurnB,
        Finished,
        Idle
    }

    [Serializable]
    public class MsgGameFinished
    {
        public string WinnerName { get; set; }
    }

    [Serializable]
    public class MsgLeaveGame
    {
    }
}
