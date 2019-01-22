using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Controller.NetWork
{
    public class SimpleUdpServer : IDisposable
    {
        #region Variable
        private Socket m_Socket;
        private IPEndPoint m_SocketEP;
        private byte[] receiveBuffer;
        private List<EndPoint> m_EPList = new List<EndPoint>();
        public int Port => m_SocketEP.Port;
        #endregion

        #region Setting
        public int ReceiveTimeout { get => m_Socket.ReceiveTimeout; set { m_Socket.ReceiveTimeout = value; } }
        public int SendTimeout { get => m_Socket.SendTimeout; set { m_Socket.SendTimeout = value; } }
        #endregion

        #region Event
        public event UdpMessageEventHandler OnMessage;
        public event ErrorEventHandler OnError;
        #endregion

        #region Constructor
        public SimpleUdpServer(IPEndPoint localEP)
        {
            m_SocketEP = new IPEndPoint(new IPAddress(localEP.Address.GetAddressBytes()), localEP.Port);
            m_Socket = new Socket(m_SocketEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            receiveBuffer = new byte[m_Socket.ReceiveBufferSize];
        }

        public SimpleUdpServer(IPAddress localaddr, int port) : this(new IPEndPoint(localaddr, port))
        { }

        public SimpleUdpServer(int port) : this(new IPEndPoint(IPAddress.Any, port))
        { }
        #endregion

        #region UdpEvent
        public void Start()
        {
            if (m_Socket is null)
            {
                m_Socket = new Socket(m_SocketEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            }
            if (!m_Socket.IsBound)
            {
                if (m_SocketEP.Address.Equals(IPAddress.Broadcast))
                {
                    m_Socket.EnableBroadcast = true;
                }
                else
                {
                    try
                    {
                        m_Socket.Bind(m_SocketEP);
                        if (m_SocketEP.Port == 0)
                        {
                            m_SocketEP.Port = ((IPEndPoint)m_Socket.LocalEndPoint).Port;
                        }
                        Receive();
                    }
                    catch (SocketException ex)
                    {
                        OnError?.Invoke(this, new ErrorEventArgs(ex));
                    }
                }
            }
        }

        private void Receive()
        {
            EndPoint m_remoteEP = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                m_Socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref m_remoteEP, ReceiveCallBack, null);
            }
            catch { }
        }

        private void ReceiveCallBack(IAsyncResult result)
        {
            try
            {
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                int dataLen = m_Socket.EndReceiveFrom(result, ref endPoint);
                _OnMessage(dataLen, endPoint);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs(ex));
            }
            finally
            {
                Receive();
            }
        }

        public void KeepEP(UdpMessageEventArgs e)
        {
            m_EPList.Add(e.RemoteEndPoint);
        }

        public void Send(string msg, EndPoint endPoint)
        {
            Send(Encoding.UTF8.GetBytes(msg), endPoint);
        }

        public void Send(byte[] data, EndPoint endPoint)
        {
            try
            {
                m_Socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, endPoint, SendCallBack, null);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        private void SendCallBack(IAsyncResult result)
        {
            EndPoint EP = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                m_Socket.EndReceiveFrom(result, ref EP);
            }
            catch { }
        }

        public void SendToAll(string msg)
        {
            SendToAll(Encoding.UTF8.GetBytes(msg));
        }

        public void SendToAll(byte[] data)
        {
            foreach (var ep in m_EPList)
            {
                Send(data, ep);
            }
        }

        public void Close()
        {
            if (m_Socket != null)
            {
                m_Socket.Close();
                m_Socket = null;
                m_EPList.Clear();
            }
        }
        #endregion

        #region InternalEvent
        private void _OnMessage(int dataLength, EndPoint endPoint)
        {
            OnMessage?.Invoke(this, new UdpMessageEventArgs(dataLength, receiveBuffer, endPoint));
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
