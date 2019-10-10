using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SBServer
{
    /// <summary>
    /// Базовый класс хоста сервера
    /// </summary>
    public class ServerHostModel
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string Name
        {
            get { return _host.Name; }
        }
        
        SBServerHost _host;

        public ServerHostModel(SBServerHost host)
        {
            _host = host;
        }

    
        private void RaizePropertyChangedEvent(string propName)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
