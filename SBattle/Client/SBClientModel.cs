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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SBattle.Client;
using NetLib.Packets;
using Common;

namespace SBattle.Client
{
    public class SBClientModel : DependencyObject
    {
        public SBClient Client { get; private set; }

        #region ObservableCollection<GameInfo> CurrentGames

        public ObservableCollection<GameInfo> CurrentGames
        {
            get { return (ObservableCollection<GameInfo>)GetValue(CurrentGamesProperty); }
            set { SetValue(CurrentGamesProperty, value); }
        }

        public static readonly DependencyProperty CurrentGamesProperty =
            DependencyProperty.Register("CurrentGames", typeof(ObservableCollection<GameInfo>), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region ObservableCollection<PlayerInfo> PlayersOnline

        public ObservableCollection<PlayerInfo> PlayersOnline
        {
            get { return (ObservableCollection<PlayerInfo>)GetValue(PlayersOnlineProperty); }
            set { SetValue(PlayersOnlineProperty, value); }
        }

        public static readonly DependencyProperty PlayersOnlineProperty =
            DependencyProperty.Register("PlayersOnline", typeof(ObservableCollection<PlayerInfo>), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        public SBClientModel(SBClient client)
        {
            this.Client = client;
            this.CurrentGames = new ObservableCollection<GameInfo>();
            this.PlayersOnline = new ObservableCollection<PlayerInfo>();
            this.Subscribe(client);
        }

        private void Subscribe(SBClient client)
        {
            client.OnAddKnownClient += info => this.Invoke(() => {
                this.PlayersOnline.Add(new PlayerInfo(info.Id, info.Name, info.State));
            });
            client.OnRemoveAllKnownClients += () => this.Invoke(() => {
                this.PlayersOnline.Clear();
            });
            client.OnRemoveKnownClient += id => this.Invoke(() => {
                this.PlayersOnline.Remove(this.PlayersOnline.FirstOrDefault(p => p.Id == id));
            });
            client.OnKnownClientStateUpdate += (id, state) => this.Invoke(() => {
                var c = this.PlayersOnline.FirstOrDefault(p => p.Id == id);
                if (c != null) c.UpdateState(state);
            });

            client.OnNewGame += (gid, ida, idb) => this.Invoke(() => {
                var pa = this.PlayersOnline.FirstOrDefault(p => p.Id == ida);
                var pb = this.PlayersOnline.FirstOrDefault(p => p.Id == idb);
                if (pa != null && pb != null) this.CurrentGames.Add(new GameInfo(gid, pa.Name, pb.Name));
            });
            client.OnUpdateGameScore += (gid, sa, sb) => this.Invoke(() => {
                var c = this.CurrentGames.FirstOrDefault(g => g.Id == gid);
                if (c != null) c.UpdateScores(sa, sb);
            });
            client.OnRemoveGame += gid => this.Invoke(() => {
                this.CurrentGames.Remove(this.CurrentGames.FirstOrDefault(g => g.Id == gid));
            });
        }
    }

    public class GameInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public Guid Id { get; private set; }

        public string NameA { get; private set; }
        public string NameB { get; private set; }

        public int ScoresA { get; private set; }
        public int ScoresB { get; private set; }

        public GameInfo(Guid id, string nameA, string nameB)
        {
            this.Id = id;
            this.NameA = nameA;
            this.NameB = nameB;
        }

        public void UpdateScores(int a, int b)
        {
            this.ScoresA = a;
            this.ScoresB = b;
            PropertyChanged(this, new PropertyChangedEventArgs("ScoresA"));
            PropertyChanged(this, new PropertyChangedEventArgs("ScoresB"));
        }
    }

    public class PlayerInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public bool ReadyForGame { get; private set; }
        public string Status { get; private set; }

        public PlayerInfo(Guid id, string name, KnownClientState state)
        {
            this.Id = id;
            this.Name = name;
            this.UpdateState(state);
        }

        public void UpdateState(KnownClientState state)
        {
            this.ReadyForGame = state == KnownClientState.Ready;
            this.Status = state == KnownClientState.Free ? string.Empty : state.ToString();
            PropertyChanged(this, new PropertyChangedEventArgs("Status"));
            PropertyChanged(this, new PropertyChangedEventArgs("ReadyForGame"));
        }
    }
}
