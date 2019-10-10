using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NetLib.Packets;
using NetLib;
using Common;

namespace SBattle.Client
{
    public class SBClient
    {
        static readonly Dispatcher<SBClient> _dispatcher = new Dispatcher<SBClient>();
        Connection _cnn;

        public event Action<string> OnChatMessage = delegate { };
        public event Action<KnownClientInfo> OnAddKnownClient = delegate { };
        public event Action<Guid> OnRemoveKnownClient = delegate { };
        public event Action OnRemoveAllKnownClients = delegate { };
        public event Action<Guid, KnownClientState> OnKnownClientStateUpdate = delegate { };
        public event Action OnSessionRestoreFailed = delegate { };
        public event Action OnConnectedSuccessful = delegate { };
        public event Action<Guid, Guid, Guid> OnNewGame = delegate { };
        public event Action<Guid> OnRemoveGame = delegate { };
        public event Action<Guid, int, int> OnUpdateGameScore = delegate { };
        public event Action OnConnectionLost = delegate { };

        public event Action OnTurn = delegate { };
        public event Action<int, int, bool> OnOpponentFire = delegate { };
        public event Action<bool, bool> OnFireResult = delegate { };
        public event Action<string> OnGameFinished = delegate { };

        public Guid Id { get; private set; }

        Func<bool> _connect;

        public SBClient(string remoteHost, ushort remotePort, string username)
        {
            _connect = () => {
                try
                {
                    var sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sck.Connect(remoteHost, remotePort);

                    _cnn = new Connection(sck);
                    _cnn.OnData += obj => {
                        System.Diagnostics.Debug.Print("[Клиент] " + obj.Collect(obj.GetType().Name));
                        _dispatcher.Dispatch(this, obj);
                    };
                    _cnn.OnConnectionLost += () => OnConnectionLost();

                    return true;
                }
                catch (SocketException ex)
                {
                    return false;
                }
            };

            if (!_connect())
                throw new ApplicationException("Ошибка соединения!");

            _cnn.Send(new MsgRegisterName() { ClientName = username });
        }

        public bool Reconnect()
        {
            bool ok;
            for (int i = 0; !(ok = _connect()) && i < 10; i++) ;
            _cnn.Send(new MsgSessionId() { Id = this.Id });
            return ok;
        }

        public void Start()
        {
            _cnn.Start();
        }

        private void Handle(MsgChatMessage pckt)
        {
            OnChatMessage(pckt.Text);
        }

        private void Handle(MsgAddKnownClients pckt)
        {
            foreach (var item in pckt.Clients)
            {
                OnAddKnownClient(item);
            }
        }

        private void Handle(MsgRemoveKnownClient pckt)
        {
            if (pckt.All)
            {
                OnRemoveAllKnownClients();
            }
            else
            {
                OnRemoveKnownClient(pckt.Id);
            }
        }

        private void Handle(MsgSessionId pckt)
        {
            if (pckt.Id == Guid.Empty)
            {
                OnSessionRestoreFailed();
            }
            else
            {
                if (this.Id == Guid.Empty)
                {
                    this.Id = pckt.Id;
                }
                else if (this.Id != pckt.Id)
                {
                    throw new NotImplementedException("");
                }

                OnConnectedSuccessful();
            }
        }

        private void Handle(MsgKnownClientStateUpdate pckt)
        {
            if (pckt.Id != this.Id)
                OnKnownClientStateUpdate(pckt.Id, pckt.State);
        }

        private void Handle(MsgNewGame pckt)
        {
            OnNewGame(pckt.Id, pckt.PlayerA, pckt.PlayerB);
        }

        private void Handle(MsgRemoveGame pckt)
        {
            OnRemoveGame(pckt.Id);
        }

        private void Handle(MsgUpdateGame pckt)
        {
            OnUpdateGameScore(pckt.Id, pckt.ScoresA, pckt.ScoresB);
        }

        private void Handle(MsgTurn pckt)
        {
            OnTurn();
        }

        private void Handle(MsgGameFinished pckt)
        {
            OnGameFinished(pckt.WinnerName);
        }

        private void Handle(MsgFireResult pckt)
        {
            OnFireResult(pckt.Miss, pckt.Dead);
        }

        private void Handle(MsgFire pckt)
        {
            OnOpponentFire(pckt.X, pckt.Y, pckt.Dead);
        }

        public void SendPlace(int x, int y, int len, bool vertical)
        {
            _cnn.Send(new MsgPlace() { X = x, Y = y, Len = len, Vertical = vertical });
        }

        public void SendChatMessage(string msg)
        {
            _cnn.Send(new MsgChatMessage() { Text = msg });
        }

        public void SendReadyForGame(bool ready, string pwd = null)
        {
            _cnn.Send(new MsgSetReadyForGame() { IsReady = ready, Password = pwd });
        }

        public void SendFire(int x, int y)
        {
            _cnn.Send(new MsgFire() { X = x, Y = y });
        }

        public void SendLeave()
        {
            _cnn.Send(new MsgLeaveGame());
        }

        public void SendBeginGame(Guid opponentId, string pwd)
        {
            _cnn.Send(new MsgBeginGame() { OpponentId = opponentId, Password = pwd });
        }
    }
}
