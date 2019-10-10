using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SBattle.Client;
using SBattle.UI;
using NetLib.Discover;
using Common;

namespace SBattle
{
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        SBClient _client;
        public SBClientModel Client { get; private set; }

        /// <summary>
        /// Состояние сражения
        /// </summary>
        enum BattleState
        {
            Init,
            Turn,
            Wait,
            Finished
        }

        BattleState _state;
        IBattleFieldCell _cell;
        bool _initVertical;
        int _initLength;
        int _initCount;
        List<IBattleFieldCell> _cells = new List<IBattleFieldCell>();

        /// <summary>
        /// Конструктор окна с игрой
        /// </summary>
        /// <param name="client"></param>
        public GameWindow(SBClientModel client)
        {
            _initLength = 4;
            _initCount = 0;
            _initVertical = false;
            _state = BattleState.Init;
            _client = client.Client;
            this.Client = client;

            InitializeComponent();

            myField.Colors = CellValues.BattleFieldColors;
            myField.BorderColors = BorderValues.BattleFieldBorderColors;

            enemyField.Colors = CellValues.BattleFieldColors;
            enemyField.BorderColors = BorderValues.BattleFieldBorderColors;

            #region мышь указатели

            enemyField.OnBattleFieldCellMouseEnter += (sender, ea) => {
                if (_state == BattleState.Turn)
                {
                    if (ea.Cell.Value == CellValues.None)
                    {
                        ea.Cell.BorderValue = BorderValues.TargetSelection;
                    }
                }
            };
            enemyField.OnBattleFieldCellMouseLeave += (sender, ea) => {
                ea.Cell.BorderValue = BorderValues.None;
            };
            enemyField.OnBattleFieldCellMouseUp += (sender, ea) => {
                if (_state == BattleState.Turn)
                {
                    if (ea.Cell.Value == CellValues.None)
                    {
                        _state = BattleState.Wait;
                        _cell = ea.Cell;
                        _client.SendFire(ea.Cell.X, ea.Cell.Y);
                    }
                }
            };

            myField.OnBattleFieldCellMouseEnter += (sender, ea) => {
                if (_state == BattleState.Init && _initLength > 0)
                {
                    var ex = _initVertical ? ea.Cell.X : ea.Cell.X + _initLength - 1;
                    var ey = _initVertical ? ea.Cell.Y + _initLength - 1 : ea.Cell.Y;

                    if (ex < 10 && ey < 10)
                    {
                        if (this.IsRectEmpty(myField, ea.Cell.X - 1, ea.Cell.Y - 1, ex + 1, ey + 1))
                        {
                            for (int x = ea.Cell.X; x <= ex; x++)
                            {
                                for (int y = ea.Cell.Y; y <= ey; y++)
                                {
                                    myField[x, y].BorderValue = BorderValues.PlaceSelection;
                                    _cells.Add(myField[x, y]);
                                }
                            }
                        }
                    }
                }
            };
            myField.OnBattleFieldCellMouseLeave += (sender, ea) => {
                if (_state == BattleState.Init && _cells.Count > 0)
                {
                    _cells.ForEach(cell => cell.BorderValue = BorderValues.None);
                    _cells.Clear();
                }
            };
            myField.OnBattleFieldCellMouseUp += (sender, ea) => {
                if (ea.Button == MouseButton.Left)
                {
                    if (_state == BattleState.Init && _initLength > 0)
                    {
                        if (_cells.Count > 0)
                        {
                            _cells.ForEach(cell => {
                                cell.BorderValue = BorderValues.None;
                                cell.Value = CellValues.Own;
                            });
                            _cells.Clear();

                            _client.SendPlace(ea.Cell.X, ea.Cell.Y, _initLength, _initVertical);

                            _initCount++;
                            if (_initLength + _initCount == 5)
                            {
                                _initLength--;
                                _initCount = 0;

                                if (_initLength == 0)
                                {
                                    _state = BattleState.Wait;
                                }
                            }
                        }
                    }
                }
                else if (tt.IsEnabled)
                {
                    _initVertical = !_initVertical;
                }
            };

            #endregion

            _client.OnOpponentFire += (x, y, dead) => {
                if (_state == BattleState.Wait)
                {
                    this.Invoke(() => {
                        if (myField[x, y].Value == CellValues.Own)
                        {
                            myField[x, y].Value = CellValues.Dead;

                            if (dead) SetCompletlyDead(myField, x, y);
                        }
                        else
                        {
                            myField[x, y].Value = CellValues.FiredEmpty;
                        }
                    });
                }
            };

            _client.OnFireResult += (miss, dead) => {
                if (_state == BattleState.Wait)
                {
                    if (_cell != null)
                    {
                        this.Invoke(() => {
                            if (miss)
                            {
                                _cell.Value = CellValues.FiredEmpty;
                            }
                            else
                            {
                                _cell.Value = CellValues.Dead;
                                if (dead) SetCompletlyDead(enemyField, _cell.X, _cell.Y);
                            }
                        });
                        _cell = null;
                    }
                    else
                    {
                        MessageBox.Show("Неожиданный результат");
                    }
                }
            };

            _client.OnGameFinished += winner => {
                if (_state == BattleState.Wait)
                {
                    _state = BattleState.Finished;
                    this.Invoke(() => {
                        MessageBox.Show(this, winner + " одержал победу!");
                        this.DialogResult = true;
                        this.Close();
                    });
                }
            };

            _client.OnTurn += () => {
                if (_state == BattleState.Wait)
                {
                    _state = BattleState.Turn;
                }
            };

            BattleState st = BattleState.Finished;

            _client.OnConnectionLost += () => {
                if (_state != BattleState.Finished)
                {
                    this.Invoke(() => chat.AppendLine("Переподключение..."));
                    st = _state;
                    _state = BattleState.Wait;

                    if (!_client.Reconnect())
                        this.Invoke(() => chat.AppendLine("Ошибка"));
                    else
                        this.Invoke(() => chat.AppendLine("Соединение установлено. Восстанавливается сеанс..."));
                }
            };

            _client.OnConnectedSuccessful += () => {
                if (_state != BattleState.Finished)
                {
                    //this.Invoke(() => chat.AppendLine("Сессия восстановлена."));
                    _state = st;
                }
            };

            _client.OnSessionRestoreFailed += () => {
                if (_state != BattleState.Finished)
                {
                    this.Invoke(() => {
                        MessageBox.Show(this, "Ошибка восстановления сессии.");
                        this.DialogResult = false;
                        this.Close();
                    });
                }
            };
        }

        struct PT { public int X, Y;}

        private void SetCompletlyDead(BattleField field, int x, int y)
        {
            var q = new Queue<PT>();
            q.Enqueue(new PT() { X = x, Y = y });

            while (q.Count > 0)
            {
                var pt = q.Dequeue();

                if (pt.X >= 0 && pt.Y >= 0 && pt.X < 10 && pt.Y < 10)
                {
                    if (field[pt.X, pt.Y].Value == CellValues.Dead)
                    {
                        q.Enqueue(new PT() { X = pt.X - 1, Y = pt.Y });
                        q.Enqueue(new PT() { X = pt.X + 1, Y = pt.Y });
                        q.Enqueue(new PT() { X = pt.X, Y = pt.Y - 1 });
                        q.Enqueue(new PT() { X = pt.X, Y = pt.Y + 1 });
                        field[pt.X, pt.Y].Value = CellValues.CompletlyDead;
                    }
                }
            }
        }

        private bool IsRectEmpty(BattleField field, int x, int y, int ex, int ey)
        {
            if (x <= 0) x = 0; else if (x >= 10) x = 9;
            if (y <= 0) y = 0; else if (y >= 10) y = 9;
            if (ex <= 0) ex = 0; else if (ex >= 10) ex = 9;
            if (ey <= 0) ey = 0; else if (ey >= 10) ey = 9;

            for (int cx = x; cx <= ex; cx++)
            {
                for (int cy = y; cy <= ey; cy++)
                {
                    if (field[cx, cy].Value != CellValues.None)
                        return false;
                }
            }

            return true;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _initVertical = !_initVertical;
        }
    }
}
