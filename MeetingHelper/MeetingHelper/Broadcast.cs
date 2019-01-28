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
        /// <summary>
        /// 訊息間隔時間。
        /// </summary>
        public int Delay { get; set; }
        private DelayCaller m_delay;
        #endregion

        #region Constructor
        /// <summary>
        /// 用給定的埠號建立廣播，預設間隔時間為250ms。
        /// </summary>
        /// <param name="port">埠號</param>
        public BroadcastTrigger(int port) : this(port, 250)
        { }
        /// <summary>
        /// 用給定的埠號和間隔時間建立廣播。
        /// </summary>
        /// <param name="port">埠號</param>
        /// <param name="millisecondsDelay">間隔時間</param>
        public BroadcastTrigger(int port, int millisecondsDelay)
        {
            Delay = millisecondsDelay;
            m_server = new SimpleUdpServer(IPAddress.Broadcast, port);
            m_buffer = new byte[0];
            m_server.Start();
            Port = m_server.Port;
        }
        #endregion

        #region Method
        /// <summary>
        /// 將指定字串設定成廣播訊息以UTF-8編碼。
        /// </summary>
        /// <param name="msg">字串</param>
        public void SetSendBuffer(string msg)
        {
            SetSendBuffer(Encoding.UTF8.GetBytes(msg));
        }
        /// <summary>
        /// 將指定位元組設定成廣播訊息。
        /// </summary>
        /// <param name="data">字串</param>
        public void SetSendBuffer(byte[] data)
        {
            m_buffer = (byte[])data.Clone();
        }
        /// <summary>
        /// 開啟廣播，於指定時間後關閉。
        /// </summary>
        /// <param name="millisecondsDelay">持續時間</param>
        public void StartBroadcast(int millisecondsDelay)
        {
            if (m_buffer == null) m_buffer = new byte[0];
            if (!isBroadcast)
            {
                isBroadcast = true;
                Task.Factory.StartNew(BroadcastProcess);
            }
            if (millisecondsDelay >= 0)
            {
                if (m_delay == null)
                {
                    m_delay = new DelayCaller(millisecondsDelay, StopBroadcast);
                }
                else
                {
                    m_delay.Reset(millisecondsDelay);
                }
            }
        }
        /// <summary>
        /// 開啟無限制時間的廣播。
        /// </summary>
        public void StartBroadcast()
        {
            StartBroadcast(-1);
        }

        private async void BroadcastProcess()
        {
            while (isBroadcast)
            {
                m_server.Send(m_buffer, new IPEndPoint(IPAddress.Broadcast, Port));
                await Task.Delay(Delay);
            }
        }
        /// <summary>
        /// 關閉廣播。
        /// </summary>
        public void StopBroadcast()
        {
            isBroadcast = false;
        }
        /// <summary>
        /// 關閉廣播並清空廣播訊息。
        /// </summary>
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
        /// <summary>
        /// 接收到訊息時觸發。
        /// </summary>
        public event UdpMessageEventHandler OnMessage;
        #endregion

        #region Constructor
        /// <summary>
        /// 監聽指定埠號的廣播。
        /// </summary>
        /// <param name="port">埠號</param>
        public BroadcastListener(int port)
        {
            m_client = new SimpleUdpServer(IPAddress.Any, port);
            m_client.OnMessage += _OnMessage;
        }
        #endregion

        #region Method
        /// <summary>
        /// 開始監聽廣播訊息。
        /// </summary>
        public void Start()
        {
            m_client.Start();
        }
        /// <summary>
        /// 內部傳送事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _OnMessage(object sender, UdpMessageEventArgs e)
        {
            OnMessage?.Invoke(this, e);
        }
        /// <summary>
        /// 停止監聽訊息。
        /// </summary>
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
