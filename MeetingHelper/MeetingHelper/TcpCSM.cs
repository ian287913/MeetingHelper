/* TcpCSM => Tcp Client - Server Model */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Controller.NetWork
{
    public class SimpleTcpServer : IDisposable
    {
        #region Variable
        private readonly IPEndPoint m_SocketEP;
        private Socket m_Socket;
        private List<SimpleTcpClient> m_ConnList = new List<SimpleTcpClient>();
        private bool m_Active = false;
        #endregion

        #region Setting
        public int ReceiveTimeout { get => m_Socket.ReceiveTimeout; set { m_Socket.ReceiveTimeout = value; } }
        public int SendTimeout { get => m_Socket.SendTimeout; set { m_Socket.SendTimeout = value; } }
        #endregion

        #region Event
        public event EventHandler OnAccept;
        public event TcpMessageEventHandler OnMessage;
        public event ErrorEventHandler OnError;
        public event ErrorEventHandler OnClientError;
        public event EventHandler OnClose;
        #endregion

        #region Constructor
        public SimpleTcpServer(IPEndPoint localEP)
        {
            m_SocketEP = new IPEndPoint(new IPAddress(localEP.Address.GetAddressBytes()), localEP.Port); ;
            m_Socket = new Socket(m_SocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public SimpleTcpServer(IPAddress localaddr, int port) : this(new IPEndPoint(localaddr, port))
        { }

        public SimpleTcpServer(int port) : this(new IPEndPoint(IPAddress.Any, port))
        { }
        #endregion

        #region TcpEvent
        public void Start()
        {
            Start((int)SocketOptionName.MaxConnections);
        }

        public void Start(int backlog)
        {
            if (!m_Active && m_Socket == null)
            {
                m_Socket = new Socket(m_SocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            try
            {
                if (!m_Active)
                {
                    m_Socket.Bind(m_SocketEP);
                    m_Socket.Listen(backlog);
                    m_Active = true;
                    Accept();
                }
            }
            catch (SocketException ex)
            {
                Stop();
                _OnError(ex);
            }
        }

        private void Accept()
        {
            if (m_Active)
            {
                m_Socket.BeginAccept(AcceptCallBack, null);
            }
            else
            {
                _OnError(new InvalidOperationException("尚未呼叫 Start() 來啟動接聽項。"));
            }
        }

        private void AcceptCallBack(IAsyncResult result)
        {
            if (m_Socket != null)
            {
                try
                {
                    SimpleTcpClient conn = new SimpleTcpClient(m_Socket.EndAccept(result));
                    _OnAccept(conn);
                    Accept();
                }
                catch (Exception) { }
            }
        }

        public void SendToAll(string msg)
        {
            SendToAll(Encoding.UTF8.GetBytes(msg));
        }

        public void SendToAll(byte[] data)
        {
            foreach (var conn in m_ConnList)
            {
                conn.Send(data);
            }
        }

        public void Stop()
        {
            foreach (var conn in m_ConnList)
            {
                conn.Close();
            }
            m_ConnList.Clear();
            m_Socket?.Close();
            m_Socket = null;
            m_Active = false;
        }

        public void KeepAlive(SimpleTcpClient conn)
        {
            m_ConnList.Add(conn);
        }

        public async void KeepAlive(SimpleTcpClient conn, int ttl)
        {
            m_ConnList.Add(conn);
            conn.TTL = ttl;
            await Task.Factory.StartNew(new Action<object>(RemoveConn), conn);
        }

        private async void RemoveConn(object obj)
        {
            SimpleTcpClient conn = obj as SimpleTcpClient;
            await Task.Delay(conn.TTL);
            conn.Close();
            m_ConnList.Remove(conn);
        }

        private void AddClientEvent(SimpleTcpClient conn)
        {
            conn.OnMessage += _OnMessage;
            conn.OnClose += _OnClose;
            conn.OnError += _OnClientError;
            conn.Receive();
        }
        #endregion

        #region InternalEvent
        private void _OnAccept(SimpleTcpClient conn)
        {
            AddClientEvent(conn);
            OnAccept?.Invoke(conn, EventArgs.Empty);
            if (m_ConnList.LastIndexOf(conn) == -1)
            {
                conn.Close();
            }
        }

        private void _OnMessage(object conn, TcpMessageEventArgs e)
        {
            OnMessage?.Invoke(conn, e);
        }

        private void _OnError(Exception ex)
        {
            OnError?.Invoke(this, new ErrorEventArgs(ex));
        }

        private void _OnClientError(object conn, ErrorEventArgs e)
        {
            OnClientError?.Invoke(conn, e);
        }

        private void _OnClose(object sender, EventArgs e)
        {
            SimpleTcpClient conn = sender as SimpleTcpClient;
            conn.Close();
            m_ConnList.Remove(conn);
            OnClose?.Invoke(conn, e);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                    m_Socket.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class SimpleTcpClient : IDisposable
    {
        #region Variable
        private readonly IPEndPoint m_SocketEP;
        private Socket m_Socket;
        private bool m_Active = false;
        private byte[] receiveBuffer;
        private bool receiveFixedByte = false;
        public IPAddress Address => new IPAddress(m_SocketEP.Address.GetAddressBytes());
        internal int TTL { get; set; } = -1;
        public bool IsConnected { get; private set; }
        #endregion

        #region Setting
        public int ReceiveTimeout { get => m_Socket.ReceiveTimeout; set { m_Socket.ReceiveTimeout = value; } }
        public int SendTimeout { get => m_Socket.SendTimeout; set { m_Socket.SendTimeout = value; } }
        #endregion

        #region Event
        public event EventHandler OnConnect;
        public event TcpMessageEventHandler OnMessage;
        public event ErrorEventHandler OnError;
        public event EventHandler OnClose;
        #endregion

        #region Constructor
        public SimpleTcpClient(IPEndPoint localEP)
        {
            m_SocketEP = new IPEndPoint(new IPAddress(localEP.Address.GetAddressBytes()), localEP.Port); ;
            m_Socket = new Socket(m_SocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            receiveBuffer = new byte[m_Socket.ReceiveBufferSize];
        }

        public SimpleTcpClient(IPAddress localaddr, int port) : this(new IPEndPoint(localaddr, port))
        { }

        internal SimpleTcpClient(Socket acceptedScoket)
        {
            m_SocketEP = (IPEndPoint)acceptedScoket.LocalEndPoint;
            m_Socket = acceptedScoket;
            m_Active = true;
            receiveBuffer = new byte[m_Socket.ReceiveBufferSize];
        }
        #endregion

        #region TcpEvent
        public void Connect()
        {
            try
            {
                if (m_Socket == null)
                {
                    m_Socket = new Socket(m_SocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }
                m_Socket.BeginConnect(m_SocketEP, ConnectCallBack, null);
                m_Active = true;
            }
            catch (SocketException ex)
            {
                _OnError(ex);
            }
        }

        private void ConnectCallBack(IAsyncResult result)
        {
            try
            {
                m_Socket.EndConnect(result);
                _OnConnect();
                Receive();
            }
            catch (SocketException ex)
            {
                _OnError(ex);
                Close();
            }
        }

        public ArraySegment<byte> ReceiveBytes(int maxSize)
        {
            receiveFixedByte = true;
            byte[] buf = new byte[maxSize];
            int dataLength = m_Socket.Receive(buf);
            Receive();
            return new ArraySegment<byte>(buf, 0, dataLength);
        }

        internal void Receive()
        {
            m_Socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallBack, null);
        }

        private void ReceiveCallBack(IAsyncResult result)
        {
            receiveFixedByte = false;
            try
            {
                int len = m_Socket.EndReceive(result);
                if (len == 0 && m_Socket.Available == 0 || m_Socket.Connected == false)
                {
                    Close();
                }
                else
                {
                    _OnMessage(len);
                    if (!receiveFixedByte)
                    {
                        Receive();
                    }
                }
            }
            catch (Exception ex)
            {
                if (m_Active) _OnError(ex);
                Close();
            }
        }

        public void Send(string msg)
        {
            Send(Encoding.UTF8.GetBytes(msg));
        }

        public void Send(byte[] data)
        {
            if (m_Socket.Connected && m_Active)
            {
                m_Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallBack, null);
            }
            else
            {
                Close();
            }
        }

        private void SendCallBack(IAsyncResult result)
        {
            if (m_Active)
                m_Socket.EndSend(result);
        }

        public void Close()
        {
            try
            {
                if (m_Active)
                {
                    m_Socket.BeginDisconnect(true, DisconnectCallBack, null);
                    m_Active = false;
                }
            }
            catch (Exception ex)
            {
                if (!m_Active) _OnError(ex);
            }
        }

        private void DisconnectCallBack(IAsyncResult result)
        {
            try
            {
                m_Socket.EndDisconnect(result);
            }
            catch { };
            m_Socket.Close();
            m_Socket = null;
            _OnClose();
        }
        #endregion

        #region InternalEvent
        private void _OnConnect()
        {
            IsConnected = true;
            OnConnect?.Invoke(this, EventArgs.Empty);
        }

        private void _OnMessage(int dataLen)
        {
            OnMessage?.Invoke(this, new TcpMessageEventArgs(dataLen, receiveBuffer));
        }

        private void _OnError(Exception ex)
        {
            OnError?.Invoke(this, new ErrorEventArgs(ex));
        }

        private void _OnClose()
        {
            IsConnected = false;
            OnClose?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                    m_Socket.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
