using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
namespace Controller.NetWork
{
    public class BroadcastTrigger : IDisposable
    {
        #region Variable
        private SimpleUdpServer m_server;
        private byte[] m_buffer;
        public int Port { get; private set; }
        private bool isBroadcast = false;
        public int Delay { get; set; }
        #endregion

        #region Constructor
        public BroadcastTrigger(int port) : this(port, 250)
        { }

        public BroadcastTrigger(int port, int millsecondsDelay)
        {
            Delay = millsecondsDelay;
            m_server = new SimpleUdpServer(IPAddress.Broadcast, port);
            m_buffer = new byte[0];
            m_server.Start();
            Port = m_server.Port;
        }
        #endregion

        #region Method
        public void SetSendBuffer(string msg)
        {
            SetSendBuffer(Encoding.UTF8.GetBytes(msg));
        }

        public void SetSendBuffer(byte[] data)
        {
            m_buffer = (byte[])data.Clone();
        }

        public void StartBroadcast(int secondsDelay)
        {
            if (m_buffer is null) m_buffer = new byte[0];
            if (!isBroadcast)
            {
                isBroadcast = true;
                Task.Factory.StartNew(BroadcastProcess);
                if (secondsDelay > 0)
                {
                    DelayToClose(secondsDelay * 1000);
                }
            }
        }

        public void StartBroadcast()
        {
            StartBroadcast(0);
        }

        private async void BroadcastProcess()
        {
            while (isBroadcast)
            {
                m_server.Send(m_buffer, new IPEndPoint(IPAddress.Broadcast, Port));
                await Task.Delay(Delay);
            }
        }

        private async void DelayToClose(int time)
        {
            await Task.Delay(time);
            isBroadcast = false;
        }

        public void StopBroadcast()
        {
            isBroadcast = false;
        }

        public void Close()
        {
            StopBroadcast();
            m_buffer = null;
            m_server.Close();
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
                    m_server.Dispose();
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

    public class BroadcastListener : IDisposable
    {
        #region Variable
        private SimpleUdpServer m_client;
        #endregion

        #region Event
        public event UdpMessageEventHandler OnMessage;
        #endregion

        #region Constructor
        public BroadcastListener(int port)
        {
            m_client = new SimpleUdpServer(IPAddress.Any, port);
            m_client.OnMessage += _OnMessage;
        }
        #endregion

        #region Method
        public void Start()
        {
            m_client.Start();
        }

        private void _OnMessage(object sender, UdpMessageEventArgs e)
        {
            OnMessage?.Invoke(this, e);
        }

        public void Close()
        {
            m_client.Close();
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
                    m_client.Dispose();
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
