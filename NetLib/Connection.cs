using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace NetLib
{
    public class Connection : IDisposable
    {
        MemoryStream _outStream;
        Socket _sck;
        BinaryFormatter _formatter;
        byte[] _hbuff;

        public event Action<object> OnData = delegate { };
        public event Action OnConnectionLost = delegate { };

        public bool IsAlive { get; private set; }

        public Connection(Socket sck)
        {
            sck.NoDelay = true;
            _sck = sck;
            _formatter = new BinaryFormatter();
            _outStream = new MemoryStream();
            this.IsAlive = true;
        }

        public void Start()
        {
            if (_hbuff != null)
                throw new InvalidOperationException();

            try
            {
                _hbuff = new byte[4];
                _sck.BeginReceive(_hbuff, 0, _hbuff.Length, SocketFlags.None, RecvProc, null);
            }
            catch (SocketException ex)
            {
                ConnectionLost(ex);
            }
        }

        private void RecvProc(IAsyncResult ar)
        {
            try
            {
                var l = _sck.EndReceive(ar);
                while (l < _hbuff.Length) l += _sck.Receive(_hbuff, l, _hbuff.Length - l, SocketFlags.None);

                var len = BitConverter.ToInt32(_hbuff, 0);
                var buff = new byte[len];

                while (len > 0) len -= _sck.Receive(buff, buff.Length - len, len, SocketFlags.None);


                object obj;

                lock (_formatter)
                {
                    obj = _formatter.Deserialize(new MemoryStream(buff));
                }

                OnData(obj);

                _sck.BeginReceive(_hbuff, 0, _hbuff.Length, SocketFlags.None, RecvProc, null);
            }
            catch (SocketException ex)
            {
                ConnectionLost(ex);
            }
        }

        private void ConnectionLost(SocketException ex)
        {
            if (this.IsAlive)
            {
                this.IsAlive = false;
                OnConnectionLost();
            }
        }

        public void Send(object obj)
        {
            try
            {
                lock (_outStream)
                {
                    lock (_formatter)
                    {
                        _outStream.Position = 0;
                        _outStream.SetLength(0);
                        _formatter.Serialize(_outStream, obj);
                    }

                    var len = (int)_outStream.Length;
                    var hdr = BitConverter.GetBytes(len);
                    _sck.Send(hdr);
                    _sck.Send(_outStream.GetBuffer(), 0, len, SocketFlags.None);
                }
            }
            catch (SocketException ex)
            {
                ConnectionLost(ex);
            }
        }

        public void Dispose()
        {
            _sck.Shutdown(SocketShutdown.Both);
        }
    }
}
