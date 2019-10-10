using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Common;

namespace SBServer
{
    /// <summary>
    /// Логика работы MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Свойства хоста

        public ServerHostModel Host
        {
            get { return (ServerHostModel)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        public static readonly DependencyProperty HostProperty =
            DependencyProperty.Register("Host", typeof(ServerHostModel), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region Свойства логов

        public ObservableCollection<LogMsg> Log
        {
            get { return (ObservableCollection<LogMsg>)GetValue(LogProperty); }
            set { SetValue(LogProperty, value); }
        }

        public static readonly DependencyProperty LogProperty =
            DependencyProperty.Register("Log", typeof(ObservableCollection<LogMsg>), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region Свойства автоскролла

        public bool AutoScroll
        {
            get { return (bool)GetValue(AutoScrollProperty); }
            set { SetValue(AutoScrollProperty, value); }
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register("AutoScroll", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        #endregion

        #region Имя сервера

        public string ServerName
        {
            get { return (string)GetValue(ServerNameProperty); }
            set { SetValue(ServerNameProperty, value); }
        }

        public static readonly DependencyProperty ServerNameProperty =
            DependencyProperty.Register("ServerName", typeof(string), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        #region Свойства видимости

        public bool ServerDiscoverable
        {
            get { return (bool)GetValue(ServerDiscoverableProperty); }
            set { SetValue(ServerDiscoverableProperty, value); }
        }

        public static readonly DependencyProperty ServerDiscoverableProperty =
            DependencyProperty.Register("ServerDiscoverable", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        #endregion

        #region Свойства Остановка сервера

        public bool ServerStopped
        {
            get { return (bool)GetValue(ServerStoppedProperty); }
            set { SetValue(ServerStoppedProperty, value); }
        }

        public static readonly DependencyProperty ServerStoppedProperty =
            DependencyProperty.Register("ServerStopped", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(true));

        #endregion

        SBServerHost _host;

        /// <summary>
        /// Конструктор главного окна
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.ServerName = "Сервер " + Environment.MachineName;

            this.Log = new ObservableCollection<LogMsg>();
        }

        /// <summary>
        /// Процедура добавления лога в главное окно сервера
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="content"></param>
        private void Append(Guid id, string name, string content)
        {
            var item = new LogMsg() {
                Time = DateTime.Now,
                Id = id,
                Name = name,
                Content = content
            };
            this.Log.Add(item);

            if (this.AutoScroll)
            {
                lvwLog.ScrollIntoView(item);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ServerStoppedProperty)
            {
                if (false.Equals(e.NewValue))
                {
                    if (_host == null)
                        if (!this.Start())
                            this.ServerStopped = true;
                }
                else if (true.Equals(e.NewValue))
                {
                    if (_host != null)
                        if (!this.Stop())
                            this.ServerStopped = false;
                }
            }

            base.OnPropertyChanged(e);
        }

        /// <summary>
        /// Процедура запуска сервера
        /// </summary>
        /// <returns></returns>
        private bool Start()
        {
            try
            {
                _host = new SBServerHost(25125, this.ServerName, this.ServerDiscoverable);
                _host.OnNewConnection += (clnt, cnn) => this.Invoke(() => {
                    this.Append(clnt.Id, clnt.Name, "[Новое соединение]");

                    cnn.OnConnectionLost += () => this.Invoke(() => {
                        this.Append(clnt.Id, clnt.Name, "[Соединение потеряно]");
                    });
                });

                this.Host = new ServerHostModel(_host);
                _host.Start();

                this.Append(Guid.Empty, string.Empty, "[Сервер запущен под именем \"" + _host.Name + "\"]");
                return true;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                this.Stop();
                this.Append(Guid.Empty, string.Empty, ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Процедура остановки сервера
        /// </summary>
        /// <returns></returns>
        private bool Stop()
        {
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
                this.Append(Guid.Empty, string.Empty, "[Сервер остановлен]");
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Класс логов главного окна
    /// </summary>
    public class LogMsg
    {
        public DateTime Time { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
    }
}
