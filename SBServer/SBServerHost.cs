using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetLib;
using NetLib.Discover;
using NetLib.Packets;
using Common;

namespace SBServer
{
    /// <summary>
    /// Класс хоста сервера
    /// </summary>
    public class SBServerHost : IDisposable
    {
        DiscoverService _discoverer;
        ConnectionListener _listener;
        RWLock<LinkedList<SBRemoteClient>> _unnamedClients = new RWLock<LinkedList<SBRemoteClient>>(new LinkedList<SBRemoteClient>());
        RWLock<Dictionary<Guid, SBRemoteClient>> _knownClients = new RWLock<Dictionary<Guid, SBRemoteClient>>(new Dictionary<Guid, SBRemoteClient>());
        RWLock<Dictionary<Guid, SBGame>> _games = new RWLock<Dictionary<Guid, SBGame>>(new Dictionary<Guid, SBGame>());

        public string Name { get; private set; }

        public event Action<SBRemoteClient, Connection> OnNewConnection = delegate { };

        public SBServerHost(ushort port, string name, bool discoverable)
        {
            this.Name = name;
            if (discoverable)
                _discoverer = new DiscoverService(port, name);

            _listener = new ConnectionListener(port);
            _listener.OnNewConnection += NewConnectionHandler;
        }

        public void Start()
        {
            _listener.Start();
        }

        /// <summary>
        /// Создание нового соединения
        /// </summary>
        /// <param name="cnn"></param>
        private void NewConnectionHandler(Connection cnn)
        {
            var c = new SBRemoteClient(cnn);
            c.OnRegisterName += name => RegisterNameHandler(c);
            c.OnRestoreSession += id => RestoreSessionHandler(id, cnn);
            c.OnChatMessage += msg => ChatMessageHandler(c, msg);
            c.OnStateChanged += st => StateChangedHandler(c, st);
            c.OnBeginGame += (opponentId, pwd) => BeginGameHandler(c, opponentId, pwd);
            c.OnConnectionLost += () => ConnectionLostHandler(c);
            c.Start();

            using (_unnamedClients.Write())
            {
                _unnamedClients.Object.AddLast(c);
            }

            OnNewConnection(c, cnn);
        }

        /// <summary>
        /// Потеря соединения
        /// </summary>
        /// <param name="c"></param>
        private void ConnectionLostHandler(SBRemoteClient c)
        {
            if (c.Name != null)
            {
                this.SendToAll(new MsgChatMessage() { Text = c.Name + " соединение потеряно" });

                if (c.State != KnownClientState.InGame)
                {
                    this.SendToAll(new MsgRemoveKnownClient() { Id = c.Id });

                    using (_knownClients.Write())
                    {
                        _knownClients.Object.Remove(c.Id);
                    }
                }
            }
            else
            {
                using (_unnamedClients.Write())
                {
                    _unnamedClients.Object.Remove(c);
                }
            }
        }

        /// <summary>
        /// Начало игры
        /// </summary>
        /// <param name="c"></param>
        /// <param name="opponentId"></param>
        /// <param name="password"></param>
        private void BeginGameHandler(SBRemoteClient c, Guid opponentId, string password)
        {
            SBRemoteClient opponent;

            if (c.State == KnownClientState.Free || c.State == KnownClientState.Ready)
            {
                using (_knownClients.Read())
                {
                    if (!_knownClients.Object.TryGetValue(opponentId, out opponent))
                        opponent = null;
                }

                if (opponent != null)
                {
                    if (opponent.Password == password)
                    {
                        c.SetState(KnownClientState.InGame);
                        opponent.SetState(KnownClientState.InGame);

                        var g = new SBGame(c, opponent);
                        g.OnScoreChanged += gc => GameScoreChangedHalder(gc);
                        g.OnGameFinished += () => GameFinishedHandler(g);

                        using (_games.Write())
                        {
                            _games.Object.Add(g.Id, g);
                        }

                        this.SendToAll(new MsgNewGame() {
                            Id = g.Id,
                            PlayerA = g.PlayerA,
                            PlayerB = g.PlayerB
                        });
                    }
                    else
                    {
                        c.Send(new MsgChatMessage() { Text = "Неверный пароль!" });
                    }
                }
            }
        }

        /// <summary>
        /// Восстановление соединения
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cnn"></param>
        private void RestoreSessionHandler(Guid id, Connection cnn)
        {
            SBRemoteClient client;

            using (_knownClients.Write())
            {
                if (!_knownClients.Object.TryGetValue(id, out client))
                    client = null;
            }

            if (client != null)
            {
                if (client.RestoreSession(cnn))
                {
                    client.Send(new MsgSessionId() { Id = id });
                    this.SendToAll(new MsgChatMessage() { Text = client.Name + " соединение восстановлено" });
                }
                else
                {
                    cnn.Send(new MsgSessionId() { Id = Guid.Empty });
                    cnn.Dispose();
                }
            }
            else
            {
                cnn.Send(new MsgSessionId() { Id = Guid.Empty });
                cnn.Dispose();
            }
        }

        /// <summary>
        /// Регистрация имени
        /// </summary>
        /// <param name="clnt"></param>
        private void RegisterNameHandler(SBRemoteClient clnt)
        {
            using (_unnamedClients.Write())
            {
                _unnamedClients.Object.Remove(clnt);
            }

            KnownClientInfo[] clients;
            using (_knownClients.Read())
            {
                clients = _knownClients.Object.Values.Select(
                    c => new KnownClientInfo() { Id = c.Id, Name = c.Name, State = c.State }
                ).ToArray();
            }
            clnt.Send(new MsgAddKnownClients() {
                Clients = clients
            });

            Tuple<MsgNewGame, MsgUpdateGame>[] games;
            using (_games.Read())
            {
                games = _games.Object.Values.Select(
                    g => Tuple.Create(
                        new MsgNewGame() {
                            Id = g.Id,
                            PlayerA = g.PlayerA,
                            PlayerB = g.PlayerB
                        },
                        new MsgUpdateGame() {
                            Id = g.Id,
                            ScoresA = g.ScoresA,
                            ScoresB = g.ScoresB
                        }
                    )
                ).ToArray();
            }
            Array.ForEach(games, g => {
                clnt.Send(g.Item1);
                clnt.Send(g.Item2);
            });

            using (_knownClients.Write())
            {
                _knownClients.Object.Add(clnt.Id, clnt);
            }

            SendToAll(new MsgAddKnownClients() {
                Clients = new[]{
                    new KnownClientInfo() { Id = clnt.Id, Name = clnt.Name, State = clnt.State }
                }
            });
        }

        /// <summary>
        /// Сообщения в чат
        /// </summary>
        /// <param name="source"></param>
        /// <param name="msg"></param>
        private void ChatMessageHandler(SBRemoteClient source, string msg)
        {
            if (source.State != KnownClientState.InGame)
            {
                this.SendToAll(new MsgChatMessage() { Text = source.Name + " - " + msg });
            }
        }

        private void StateChangedHandler(SBRemoteClient source, KnownClientState st)
        {
            this.SendToAll(new MsgKnownClientStateUpdate() { Id = source.Id, State = st });
        }

        /// <summary>
        /// Результаты игры
        /// </summary>
        /// <param name="pckt"></param>
        private void GameScoreChangedHalder(MsgUpdateGame pckt)
        {
            this.SendToAll(pckt);
        }

        /// <summary>
        /// Окончание игры
        /// </summary>
        /// <param name="g"></param>
        private void GameFinishedHandler(SBGame g)
        {
            using (_games.Write())
            {
                _games.Object.Remove(g.Id);
            }
            this.SendToAll(new MsgRemoveGame() { Id = g.Id });
        }

        /// <summary>
        /// Отправка состояний всем играющим удаленным клиентам
        /// </summary>
        /// <param name="obj"></param>
        private void SendToAll(object obj)
        {
            SBRemoteClient[] clients;

            using (_knownClients.Read())
            {
                clients = _knownClients.Object.Values.Where(
                    item => !(item.State == KnownClientState.InGame && obj is MsgChatMessage)
                ).ToArray();
            }

            Array.ForEach(clients, c => c.Send(obj));
        }

        /// <summary>
        /// Освобождение памяти
        /// </summary>
        public void Dispose()
        {
            _listener.Dispose();
            if (_discoverer != null)
                _discoverer.Dispose();
        }
    }
}
