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
using Common;

namespace SBattle.UI
{
    /// <summary>
    /// ChatFrame.xaml
    /// </summary>
    public partial class ChatFrame : UserControl
    {
        #region string ChatMessageText

        public string ChatMessageText
        {
            get { return (string)GetValue(ChatMessageTextProperty); }
            set { SetValue(ChatMessageTextProperty, value); }
        }

        public static readonly DependencyProperty ChatMessageTextProperty =
            DependencyProperty.Register("ChatMessageText", typeof(string), typeof(ChatFrame), new UIPropertyMetadata(null));

        #endregion

        #region string ChatLog

        public string ChatLog
        {
            get { return (string)GetValue(ChatLogProperty); }
            set { SetValue(ChatLogProperty, value); }
        }

        public static readonly DependencyProperty ChatLogProperty =
            DependencyProperty.Register("ChatLog", typeof(string), typeof(ChatFrame), new UIPropertyMetadata(null));

        #endregion

        #region SBClientModel Client

        public SBClientModel Client
        {
            get { return (SBClientModel)GetValue(ClientProperty); }
            set { SetValue(ClientProperty, value); }
        }

        public static readonly DependencyProperty ClientProperty =
            DependencyProperty.Register("Client", typeof(SBClientModel), typeof(ChatFrame), new UIPropertyMetadata(null));

        #endregion

        public ChatFrame()
        {
            InitializeComponent();
        }

        private void btnSendChatMessage_Click(object sender, RoutedEventArgs e)
        {
            this.Client.Client.SendChatMessage(this.ChatMessageText);
            this.ChatMessageText = "";
        }

        public void AppendLine(string line)
        {
            this.ChatLog += Environment.NewLine + "[" + DateTime.Now.ToString() + "] " + line;
            txtChatLog.ScrollToEnd();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ClientProperty)
            {
                if (e.OldValue != null)
                {
                    var model = (SBClientModel)e.OldValue;
                    model.Client.OnChatMessage -= OnIncMessageHandler;
                }

                this.Client.Client.OnChatMessage += OnIncMessageHandler;
            }

            base.OnPropertyChanged(e);
        }

        private void OnIncMessageHandler(string msg)
        {
            this.Invoke(() => this.AppendLine(msg));
        }

        private void txtChatMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnSendChatMessage_Click(null, null);
            }
        }
    }
}
