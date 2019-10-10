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
using SBattle.Client;
using NetLib.Packets;
using Common;

namespace SBattle
{
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region SBClientModel Client

        public SBClientModel Client
        {
            get { return (SBClientModel)GetValue(ClientProperty); }
            set { SetValue(ClientProperty, value); }
        }

        public static readonly DependencyProperty ClientProperty =
            DependencyProperty.Register("Client", typeof(SBClientModel), typeof(MainWindow), new UIPropertyMetadata(null));

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            var player = (PlayerInfo)((FrameworkElement)sender).DataContext;
            this.Client.Client.SendBeginGame(player.Id, pwdPassword.Password);
        }

        private void btnExpectForGame_Click(object sender, RoutedEventArgs e)
        {
            this.Client.Client.SendReadyForGame(true, pwdPassword.Password);
            btnExpectForGame.Visibility = Visibility.Hidden;
            btnCancelExpectation.Visibility = Visibility.Visible;
            pwdPassword.IsEnabled = false;
        }

        private void btnCancelExpectation_Click(object sender, RoutedEventArgs e)
        {
            this.Client.Client.SendReadyForGame(false);
            CancelExpectationUIState();
        }

        private void CancelExpectationUIState()
        {
            btnExpectForGame.Visibility = Visibility.Visible;
            btnCancelExpectation.Visibility = Visibility.Hidden;
            pwdPassword.IsEnabled = true;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ClientProperty)
            {
                this.Client.Client.OnNewGame += (gid, a, b) => this.InvokeAsync(() => {
                    if (a == this.Client.Client.Id || b == this.Client.Client.Id)
                    {
                        this.Hide();
                        if (new GameWindow(this.Client).ShowDialog() == true)
                        {
                            this.Client.Client.SendLeave();
                            this.Show();
                            CancelExpectationUIState();
                        }
                        else
                        {
                            this.Close();
                        }
                    }
                });
            }

            base.OnPropertyChanged(e);
        }
    }
}
