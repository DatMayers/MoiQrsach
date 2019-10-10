using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLib.Packets
{
    [Serializable]
    public class MsgAddKnownClients
    {
        public KnownClientInfo[] Clients { get; set; }
    }

    [Serializable]
    public class MsgRemoveKnownClient
    {
        public Guid Id { get; set; }
        public bool All { get { return this.Id == Guid.Empty; } }
    }

    [Serializable]
    public class MsgKnownClientStateUpdate
    {
        public Guid Id { get; set; }
        public KnownClientState State { get; set; }
    }

    [Serializable]
    public class KnownClientInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public KnownClientState State { get; set; }
    }

    [Serializable]
    public enum KnownClientState
    {
        Free,
        Ready,
        InGame
    }
}
