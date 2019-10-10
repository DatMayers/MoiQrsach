using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetLib.Packets;

namespace SBServer
{
    /// <summary>
    /// Класс игры
    /// </summary>
    public class SBGame
    {
        enum CellState
        {
            Free,
            FreeFired,
            Alive,
            Dead
        }

        /// <summary>
        /// Класс, описывающий игроков
        /// </summary>
        class Player
        {
            public GameState TurnState { get; set; }
            public SBRemoteClient Client { get; set; }
            public int Score { get; set; }
            public CellState[,] Field { get; set; }
            public int LeftSpace { get; set; }

            public Player()
            {
                this.Score = 0;
                this.Field = new CellState[10, 10];
                this.LeftSpace = 20;
            }
        }

        object _syncRoot = new object();

        public event Action<MsgUpdateGame> OnScoreChanged = delegate { };
        public event Action OnGameFinished = delegate { };

        public Guid Id { get; private set; }

        Player _a;
        Player _b;

        public Guid PlayerA { get { return _a.Client.Id; } }
        public Guid PlayerB { get { return _b.Client.Id; } }

        public int ScoresA { get { return _a.Score; } }
        public int ScoresB { get { return _b.Score; } }

        public GameState State { get; private set; }

        /// <summary>
        /// Конструктор класса игры. В качестве входных параметров идут классы с информацией об удаленном клиенте игроков.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public SBGame(SBRemoteClient a, SBRemoteClient b)
        {
            _a = new Player() { Client = a, TurnState = GameState.TurnA };
            _b = new Player() { Client = b, TurnState = GameState.TurnB };

            this.Subscribe(a);
            this.Subscribe(b);

            this.Id = Guid.NewGuid();
            this.State = GameState.Init;
        }

        private void Subscribe(SBRemoteClient c)
        {
            c.OnChatMessage += msg => ChatMessageHandler(c, msg);
            c.OnFire += (x, y) => FireHandler(c, x, y);
            c.OnPlace += (x, y, l, v) => PlaceHandler(c, x, y, l, v);
            c.OnLeaveGame += () => LeaveHandler(c);
            c.OnConnectionLost += () => ConnectionLostHandler(c);
        }

        /// <summary>
        /// Процедура, выполняющаяся при потере соединения
        /// </summary>
        /// <param name="source"></param>
        private void ConnectionLostHandler(SBRemoteClient source)
        {
            lock (_syncRoot)
            {
                if (this.State != GameState.Idle)
                {
                    Player p = _a.Client == source ? _a : _b;
                    Player o = _a.Client == source ? _b : _a;

                    if (o.Client.State == KnownClientState.InGame)
                    {
                        o.Client.Send(new MsgChatMessage() { Text = "У вашего противника проблемы с соединением. Ждите..." });
                    }
                }
            }
        }

        /// <summary>
        /// Процедура, выполняющаяся при выходе из игры одного из игроков
        /// </summary>
        /// <param name="source"></param>
        private void LeaveHandler(SBRemoteClient source)
        {
            lock (_syncRoot)
            {
                if (this.State != GameState.Idle)
                {
                    Player p = _a.Client == source ? _a : _b;
                    Player o = _a.Client == source ? _b : _a;

                    if (o.Client.State == KnownClientState.InGame)
                    {
                        o.Client.Send(new MsgChatMessage() { Text = "Ваш противник покинул игру!" });
                    }

                    source.SetState(KnownClientState.Free);

                    if (_a.Client.State != KnownClientState.InGame && _b.Client.State != KnownClientState.InGame)
                    {
                        this.SetState(GameState.Idle);
                    }
                }
            }
        }

        /// <summary>
        /// Сообщения в чат
        /// </summary>
        /// <param name="source"></param>
        /// <param name="msg"></param>
        private void ChatMessageHandler(SBRemoteClient source, string msg)
        {
            lock (_syncRoot)
            {
                if (source.State == KnownClientState.InGame && this.State != GameState.Idle)
                {
                    var pckt = new MsgChatMessage() { Text = source.Name + " - " + msg };
                    _a.Client.Send(pckt);
                    _b.Client.Send(pckt);
                }
            }
        }

        /// <summary>
        /// Процедура, вызывающаяся во время сражения
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void FireHandler(SBRemoteClient source, int x, int y)
        {
            lock (_syncRoot)
            {
                Player p = _a.Client == source ? _a : _b;
                Player o = _a.Client == source ? _b : _a;

                if (this.State == p.TurnState)
                {
                    switch (o.Field[x, y])
                    {
                        case CellState.Dead:
                        case CellState.FreeFired:
                            break;
                        case CellState.Free:
                            {
                                o.Field[x, y] = CellState.FreeFired;

                                o.Client.Send(new MsgFire() { X = x, Y = y, Dead = false });
                                p.Client.Send(new MsgFireResult() { Miss = true, Dead = false });

                                this.SetState(o.TurnState);
                            } break;
                        case CellState.Alive:
                            {
                                o.Field[x, y] = CellState.Dead;
                                var dead = this.CheckIsDead(o.Field, x, y);

                                o.Client.Send(new MsgFire() { X = x, Y = y, Dead = dead });
                                p.Client.Send(new MsgFireResult() { Miss = false, Dead = dead });
                                p.Score++;

                                OnScoreChanged(new MsgUpdateGame() { Id = this.Id, ScoresA = _a.Score, ScoresB = _b.Score });

                                if (p.Score >= 20)
                                {
                                    this.SetState(GameState.Finished);
                                    OnGameFinished();
                                    p.Client.Send(new MsgGameFinished() { WinnerName = p.Client.Name });
                                    o.Client.Send(new MsgGameFinished() { WinnerName = p.Client.Name });
                                }
                                else
                                {
                                    this.SetState(p.TurnState);
                                }
                            } break;
                        default:
                            throw new NotImplementedException("");
                    }
                }
            }
        }

        struct PT { public int X, Y;}

        private bool CheckIsDead(CellState[,] field, int x, int y)
        {
            var q = new Queue<PT>();
            var mask = new bool[10, 10];
            q.Enqueue(new PT() { X = x, Y = y });

            while (q.Count > 0)
            {
                var pt = q.Dequeue();

                if (pt.X >= 0 && pt.Y >= 0 && pt.X < 10 && pt.Y < 10)
                {
                    if (!mask[pt.X, pt.Y])
                    {
                        mask[pt.X, pt.Y] = true;

                        switch (field[pt.X, pt.Y])
                        {
                            case CellState.Alive: 
                                return false;
                            case CellState.Free: 
                            case CellState.FreeFired:
                                continue;
                            case CellState.Dead:
                                {
                                    q.Enqueue(new PT() { X = pt.X - 1, Y = pt.Y });
                                    q.Enqueue(new PT() { X = pt.X + 1, Y = pt.Y });
                                    q.Enqueue(new PT() { X = pt.X, Y = pt.Y - 1 });
                                    q.Enqueue(new PT() { X = pt.X, Y = pt.Y + 1 });
                                } break;
                            default:
                                throw new NotImplementedException("");
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Процедура установки состояния игры
        /// </summary>
        /// <param name="state"></param>
        private void SetState(GameState state)
        {
            this.State = state;

            switch (state)
            {
                case GameState.TurnA:
                    {
                        _a.Client.Send(new MsgTurn());
                        _a.Client.Send(new MsgChatMessage() { Text = "Твой ход" });
                        _b.Client.Send(new MsgChatMessage() { Text = "Ход противника" });
                    } break;
                case GameState.TurnB:
                    {
                        _b.Client.Send(new MsgTurn());
                        _b.Client.Send(new MsgChatMessage() { Text = "Твой ход" });
                        _a.Client.Send(new MsgChatMessage() { Text = "Ход противника" });
                    } break;
                case GameState.Finished:
                    {
                        OnGameFinished();
                    } break;
                case GameState.Idle:
                    break;
                default:
                    throw new NotImplementedException("");
            }
        }

        private void PlaceHandler(SBRemoteClient source, int x, int y, int l, bool vertical)
        {
            lock (_syncRoot)
            {
                Player p = _a.Client == source ? _a : _b;

                if (this.State == GameState.Init)
                {
                    if (p.LeftSpace >= l && l <= 4 && l >= 1)
                    {
                        p.LeftSpace -= l;

                        if (vertical)
                        {
                            for (int cy = y; cy < y + l; cy++)
                            {
                                p.Field[x, cy] = CellState.Alive;
                            }
                        }
                        else
                        {
                            for (int cx = x; cx < x + l; cx++)
                            {
                                p.Field[cx, y] = CellState.Alive;
                            }
                        }
                    }

                    if (_a.LeftSpace <= 0 && _b.LeftSpace <= 0)
                    {
                        this.SetState(GameState.TurnA);
                    }
                }
            }
        }
    }
}
