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
using Common;
using NetLib.Discover;
using SBattle.Client;

namespace SBattle
{
    /// <summary>
    /// Логика работы ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        #region Свойства выбранного сервера

        public ServerItem SelectedServer
        {
            get { return (ServerItem)GetValue(SelectedServerProperty); }
            set { SetValue(SelectedServerProperty, value); }
        }

        public static readonly DependencyProperty SelectedServerProperty =
            DependencyProperty.Register("SelectedServer", typeof(ServerItem), typeof(ConnectWindow), new UIPropertyMetadata(null));

        #endregion

        #region Сервера

        public ObservableCollection<ServerItem> Servers
        {
            get { return (ObservableCollection<ServerItem>)GetValue(ServersProperty); }
            set { SetValue(ServersProperty, value); }
        }

        public static readonly DependencyProperty ServersProperty =
            DependencyProperty.Register("Servers", typeof(ObservableCollection<ServerItem>), typeof(ConnectWindow), new UIPropertyMetadata(null));

        #endregion

        #region Адрес хоста

        public string HostAddress
        {
            get { return (string)GetValue(HostAddressProperty); }
            set { SetValue(HostAddressProperty, value); }
        }

        public static readonly DependencyProperty HostAddressProperty =
            DependencyProperty.Register("HostAddress", typeof(string), typeof(ConnectWindow), new UIPropertyMetadata(null));

        #endregion

        #region Имя игрока

        public string PlayerName
        {
            get { return (string)GetValue(PlayerNameProperty); }
            set { SetValue(PlayerNameProperty, value); }
        }

        public static readonly DependencyProperty PlayerNameProperty =
            DependencyProperty.Register("PlayerName", typeof(string), typeof(ConnectWindow), new UIPropertyMetadata(null));

        #endregion

        DiscoverClient _discoverer;

        /// <summary>
        /// Конструктор окна подключения
        /// </summary>
        public ConnectWindow()
        {
            InitializeComponent();

            this.PlayerName = Environment.UserName;
            this.Servers = new ObservableCollection<ServerItem>();

            _discoverer = new DiscoverClient(Guid.Empty, 25125);
            _discoverer.OnNewResponse += item => this.Invoke(() => this.Servers.Add(item));
            _discoverer.Reset();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == SelectedServerProperty)
            {
                if (this.SelectedServer != null)
                    this.HostAddress = this.SelectedServer.Address;
            }

            base.OnPropertyChanged(e);
        }

        /// <summary>
        /// Событие при нажатии на кнопку Обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            this.Servers.Clear();
            _discoverer.Reset();
        }

        /// <summary>
        /// Событие при нажатии на кнопку Подключиться
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clnt = new SBClient(this.HostAddress, 25125, this.PlayerName);

                new MainWindow() {
                    Client = new SBClientModel(clnt)
                }.Show();

                clnt.Start();

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void lvwServers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.SelectedServer != null)
            {
                btnConnect_Click(null, null);
            }
        }
    }
}
