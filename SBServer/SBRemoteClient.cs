using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetLib;
using NetLib.Packets;

namespace SBServer
{
    /// <summary>
    /// Класс удаленного клиента
    /// </summary>
    public class SBRemoteClient
    {
        static readonly Dispatcher<SBRemoteClient> _dispatcher = new Dispatcher<SBRemoteClient>();

        Connection _cnn;

        public event Action OnConnectionLost = delegate { };
        public event Action<string> OnRegisterName = delegate { };
        public event Action<string> OnChatMessage = delegate { };
        public event Action<Guid> OnRestoreSession = delegate { };
        public event Action<KnownClientState> OnStateChanged = delegate { };
        public event Action<Guid, string> OnBeginGame = delegate { };
        public event Action<int, int> OnFire = delegate { };
        public event Action<int, int, int, bool> OnPlace = delegate { };
        public event Action OnLeaveGame = delegate { };

        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public KnownClientState State { get; private set; }
        public string Password { get; private set; }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="cnn"></param>
        public SBRemoteClient(Connection cnn)
        {
            _cnn = cnn;
            _cnn.OnData += obj => _dispatcher.Dispatch(this, obj);
            _cnn.OnConnectionLost += () => OnConnectionLost();

            this.Id = Guid.NewGuid();
            this.State = KnownClientState.Free;
        }

        /// <summary>
        /// Запуск
        /// </summary>
        public void Start()
        {
            _cnn.Start();
        }

        private void Handle(MsgRegisterName pckt)
        {
            if (this.Name == null)
            {
                _cnn.Send(new MsgSessionId() { Id = this.Id });
                this.Name = pckt.ClientName ?? "Без имени";
                OnRegisterName(this.Name);
            }
        }

        private void Handle(MsgChatMessage pckt)
        {
            OnChatMessage(pckt.Text);
        }

        private void Handle(MsgSessionId pckt)
        {
            OnRestoreSession(pckt.Id);
        }

        private void Handle(MsgSetReadyForGame pckt)
        {
            if (pckt.IsReady)
            {
                this.SetStateInternal(KnownClientState.Ready);
            }
            else
            {
                this.SetStateInternal(KnownClientState.Free);
            }

            this.Password = pckt.Password;
        }

        private void SetStateInternal(KnownClientState state)
        {
            if ((state == KnownClientState.Ready && this.State == KnownClientState.Free) ||
                (state == KnownClientState.Free && this.State == KnownClientState.Ready))
            {
                OnStateChanged(this.State = state);
            }
        }

        public void SetState(KnownClientState state)
        {
            if ((state == KnownClientState.Free && this.State == KnownClientState.InGame) ||
                (state == KnownClientState.InGame && (this.State == KnownClientState.Free || this.State == KnownClientState.Ready)))
            {
                OnStateChanged(this.State = state);
            }
        }

        private void Handle(MsgBeginGame pckt)
        {
            if (this.State == KnownClientState.Ready || this.State == KnownClientState.Free)
            {
                OnBeginGame(pckt.OpponentId, pckt.Password);
            }
        }

        private void Handle(MsgFire pckt)
        {
            if (this.State == KnownClientState.InGame)
            {
                OnFire(pckt.X, pckt.Y);
            }
        }

        private void Handle(MsgPlace pckt)
        {
            if (this.State == KnownClientState.InGame)
            {
                OnPlace(pckt.X, pckt.Y, pckt.Len, pckt.Vertical);
            }
        }

        private void Handle(MsgLeaveGame pckt)
        {
            if (this.State == KnownClientState.InGame)
            {
                OnLeaveGame();
            }
        }

        public void Send(object obj)
        {
            _cnn.Send(obj);
        }

        /// <summary>
        /// Восстановление сессии
        /// </summary>
        /// <param name="cnn"></param>
        /// <returns></returns>
        public bool RestoreSession(Connection cnn)
        {
            if (!_cnn.IsAlive)
            {
                _cnn = cnn;
            }

            return _cnn == cnn;
        }
    }
}
