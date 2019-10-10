using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace NetLib
{
    public class ConnectionListener : IDisposable
    {
        TcpListener _listener;
        bool _started;

        public event Action<Connection> OnNewConnection = delegate { };

        public ConnectionListener(ushort port)
        {
            _listener = new TcpListener(port);
        }

        public void Start()
        {
            if (_started)
                throw new InvalidOperationException();

            _started = true;
            _listener.Start();
            _listener.BeginAcceptSocket(AcceptProc, null);            
        }

        private void AcceptProc(IAsyncResult ar)
        {
            try
            {
                var sck = _listener.EndAcceptSocket(ar);
                OnNewConnection(new Connection(sck));
                _listener.BeginAcceptSocket(AcceptProc, null);
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}
