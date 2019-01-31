using Controller.Message;
using Controller.NetWork;
using System;
using System.Collections.Generic;
using System.Net;

namespace Controller.Component
{
    public class Room : IDisposable
    {
        #region Variable
        /// <summary>
        /// 送出廣播訊號。
        /// </summary>
        private BroadcastTrigger m_trigger;
        /// <summary>
        /// 傳送指令。
        /// </summary>
        private SimpleTcpServer m_cmdSender;
        /// <summary>
        /// 傳送房間名稱。
        /// </summary>
        private SimpleTcpServer m_nameSender;
        /// <summary>
        /// 接收音訊。
        /// </summary>
        private SimpleUdpServer m_audioReceiver;
        /// <summary>
        /// 主席的連線。
        /// </summary>
        private SimpleTcpClient HostConn;
        /// <summary>
        /// 發言者的連線。
        /// </summary>
        private SimpleTcpClient SpeakerConn;
        /// <summary>
        /// Name 和 Conn 的對照。
        /// </summary>
        private readonly Map<string, SimpleTcpClient> NCMap;
        /// <summary>
        /// 密碼輸入錯誤次數。
        /// </summary>
        private readonly Dictionary<SimpleTcpClient, int> ErrorCount;
        /// <summary>
        /// 密碼最大輸入錯誤次數。
        /// </summary>
        private int m_EC = 5;
        /// <summary>
        /// 房間資訊。
        /// </summary>
        public RoomConfigure Config { get; private set; }
        /// <summary>
        /// 檢查碼。
        /// </summary>
        private readonly byte[] m_checkByte;
        /// <summary>
        /// 檢查碼(檢視用)。
        /// </summary>
        public byte CheckCode => m_checkByte[0];
        /// <summary>
        /// 密碼最大輸入錯誤次數。[1,MaxInt)
        /// </summary>
        public int MaxErrorCount
        {
            get => m_EC;
            set => m_EC = Math.Min(Math.Max(value, 1), int.MaxValue - 1);
        }
        #endregion

        #region Event
        /// <summary>
        /// 發言者發言且開啟麥克風時觸發。
        /// </summary>
        public event UdpMessageEventHandler OnAudioReceive;
        #endregion

        #region Constructor
        public Room(string RoomName, string RoomPassword)
        {
            Config = new RoomConfigure()
            {
                Name = RoomName,
                Password = RoomPassword
            };
            m_checkByte = new byte[1];
            new Random().NextBytes(m_checkByte);
            NCMap = new Map<string, SimpleTcpClient>();
            ErrorCount = new Dictionary<SimpleTcpClient, int>();

            m_trigger = new BroadcastTrigger(NetWorkPort.Broadcast);
            m_trigger.SetSendBuffer(m_checkByte);

            m_nameSender = new SimpleTcpServer(NetWorkPort.Broadcast);
            m_nameSender.OnAccept += OnGetterAccept;
            m_nameSender.OnMessage += OnNameWanted;

            m_audioReceiver = new SimpleUdpServer(NetWorkPort.Talking);
            m_audioReceiver.OnMessage += OnAudioDataReceive;

            m_cmdSender = new SimpleTcpServer(NetWorkPort.Commuting);
            m_cmdSender.OnAccept += OnAccept;
            m_cmdSender.OnClose += OnClose;
            m_cmdSender.OnMessage += OnRequestReceive;
        }
        #endregion

        #region Method
        /// <summary>
        /// 開啟廣播。
        /// </summary>
        /// <param name="duration">持續時間</param>
        /// <param name="unit">時間單位</param>
        public void StartBroadcast(int duration, TimeUnit unit)
        {
            m_nameSender.Start();
            m_trigger.StartBroadcast(duration * (int)unit);
        }

        /// <summary>
        /// 關閉廣播。
        /// </summary>
        public void StopBroadcast()
        {
            m_nameSender.Stop();
            m_trigger.StopBroadcast();
        }

        /// <summary>
        /// 開啟房間，預設是關閉麥克風。
        /// </summary>
        public void Open()
        {
            CloseMic();
            m_cmdSender.Start();
        }

        /// <summary>
        /// 關閉房間。
        /// </summary>
        public void Close()
        {
            m_cmdSender.Stop();
            CloseMic();
            StopBroadcast();
        }

        /// <summary>
        /// 開啟麥克風時，房間也會跟著打開。
        /// </summary>
        public void OpenMic()
        {
            Open();
            m_audioReceiver.Start();
        }

        /// <summary>
        /// 關閉麥克風。
        /// </summary>
        public void CloseMic()
        {
            m_audioReceiver.Close();
        }
        #endregion

        #region EventCaller
        /// <summary>
        /// 要求房間名稱的連線要求。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGetterAccept(object sender, EventArgs e)
        {
            SimpleTcpClient conn = (SimpleTcpClient)sender;
            m_nameSender.KeepAlive(conn, 3000);
        }
        /// <summary>
        /// 傳送房間名稱。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNameWanted(object sender, TcpMessageEventArgs e)
        {
            SimpleTcpClient conn = (SimpleTcpClient)sender;
            if (e.Length != m_checkByte.Length)
            {
                conn.Close();
            }
            else
            {
                for (int i = 0; i < e.Length; i++)
                {
                    if (e.Data[i] != m_checkByte[i])
                    {
                        conn.Close();
                        return;
                    }
                }
                conn.Send(Config.GetInfoBytes());
            }
        }

        /// <summary>
        /// 接收音訊資料，並檢查傳送者是否有權限發言。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAudioDataReceive(object sender, UdpMessageEventArgs e)
        {
            if (SpeakerConn != null)
            {
                IPAddress SpeakerIP = SpeakerConn.Address;
                if (SpeakerIP.Equals(e.RemoteEndPoint.Address))
                {
                    OnAudioReceive?.Invoke(this, e);
                }
            }
        }

        /// <summary>
        /// 使用者連線到主機。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAccept(object sender, EventArgs e)
        {
            m_cmdSender.KeepAlive((SimpleTcpClient)sender);
        }

        /// <summary>
        /// 接收到指令時。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRequestReceive(object sender, TcpMessageEventArgs e)
        {
            SimpleTcpClient conn = (SimpleTcpClient)sender;
            byte[] TotalData = e.Data;
            if (!Helper.IsEnough(e, out int remain))
            {
                var RemainData = conn.ReceiveBytes(remain);
                Helper.ConcatBytes(ref TotalData, ref RemainData);
            }
            Command cmd = new Command(TotalData);
            switch (cmd.Type)
            {
                #region JoinRoom
                case MessageType.JoinRoom:
                    if (cmd.Data[1] != Config.Password)
                    {
                        if (ErrorCount.TryGetValue(conn, out int ec))
                        {
                            ErrorCount[conn] = ec + 1;
                        }
                        else
                        {
                            ErrorCount[conn] = 1;
                        }
                        if (ErrorCount[conn] >= m_EC)
                        {
                            /// 密碼輸入錯誤累計到一定次數後關閉連線，成功進入才會刪除。
                            ErrorCount.Remove(conn);
                            conn.Dispose();
                        }
                        else
                        {
                            /// 傳送密碼輸入錯誤訊息。
                            conn.Send(Helper.MessageWrapper(MessageType.Unauthorized, m_EC - ErrorCount[conn]));
                        }
                    }
                    else if (NCMap.ContainsKey(cmd.Data[0]))
                    {
                        conn.Send(Helper.MessageWrapper(MessageType.Conflict));
                    }
                    else
                    {
                        if (HostConn == null)
                        {
                            HostConn = conn;
                            Config.Host = cmd.Data[0];
                            Config.CreatedAt = DateTime.UtcNow;
                        }
                        ErrorCount.Remove(conn);
                        NCMap.AddByKey(cmd.Data[0], conn);
                        conn.Send(Helper.MessageWrapper(MessageType.Success, Config.Host));
                        m_cmdSender.SendToAll(Helper.MessageWrapper(MessageType.SbJoin, cmd.Data[0]));
                    }
                    break;
                #endregion
                #region UserList
                case MessageType.UserList:
                    conn.Send(Helper.MessageWrapper(MessageType.UserList, NCMap.Keys));
                    break;
                #endregion
                #region MicCapture
                case MessageType.MicCapture:
                    if (NCMap.ContainsKey(cmd.Data[0]))
                    {
                        if (conn == HostConn)
                        {
                            /// 主席收回發言權
                            if (SpeakerConn != null)
                            {
                                SpeakerConn.Send(Helper.MessageWrapper(MessageType.MicMissing));
                            }
                            SpeakerConn = HostConn;
                            string HostName = NCMap.GetKey(HostConn);
                            HostConn.Send(Helper.MessageWrapper(MessageType.MicCapture, HostName));
                            m_cmdSender.SendToAll(Helper.MessageWrapper(MessageType.MicOwner, HostName));
                        }
                        else
                        {
                            /// 新增進要求列表。
                            HostConn.Send(Helper.MessageWrapper(MessageType.Request, cmd.Data[0]));
                            Config.AddAsker(cmd.Data[0]);
                        }
                    }
                    break;
                #endregion
                #region MicMissing
                case MessageType.MicMissing:
                    if (conn == SpeakerConn)
                    {
                        SpeakerConn.Send(Helper.MessageWrapper(MessageType.MicMissing));
                        SpeakerConn = HostConn;
                        string HostName = NCMap.GetKey(HostConn);
                        HostConn.Send(Helper.MessageWrapper(MessageType.MicCapture, NCMap.GetKey(HostConn)));
                        m_cmdSender.SendToAll(Helper.MessageWrapper(MessageType.MicOwner, HostName));
                    }
                    break;
                #endregion
                #region MicOwner
                case MessageType.MicOwner:
                    if (SpeakerConn != null)
                    {
                        conn.Send(Helper.MessageWrapper(MessageType.MicOwner, NCMap.GetKey(SpeakerConn)));
                    }
                    break;
                #endregion
                #region Accept
                case MessageType.Accept:
                    if (conn == HostConn)
                    {
                        string newSpeaker = cmd.Data[0];
                        if (Config.ContainsAsker(newSpeaker))
                        {
                            Config.RemoveAsker(newSpeaker);
                            var newSpeakerConn = NCMap[newSpeaker];
                            if (SpeakerConn != newSpeakerConn)
                            {
                                if (SpeakerConn != null)
                                {
                                    SpeakerConn.Send(Helper.MessageWrapper(MessageType.MicMissing));
                                }
                                newSpeakerConn.Send(Helper.MessageWrapper(MessageType.MicCapture));
                                SpeakerConn = newSpeakerConn;
                                m_cmdSender.SendToAll(Helper.MessageWrapper(MessageType.MicOwner, newSpeaker));
                            }
                        }
                    }
                    break;
                #endregion
                #region Refuse
                case MessageType.Refuse:
                    if (conn == HostConn)
                    {
                        string newSpeaker = cmd.Data[0];
                        if (Config.ContainsAsker(newSpeaker))
                        {
                            Config.RemoveAsker(newSpeaker);
                            if (NCMap.TryGetValue(newSpeaker, out SimpleTcpClient asker))
                            {
                                asker.Send(Helper.MessageWrapper(MessageType.Refuse));
                            }
                        }
                    }
                    break;
                #endregion
                default:
                    conn.Send(Helper.MessageWrapper(MessageType.Forbidden));
                    break;
            }
        }

        /// <summary>
        /// 使用者離開時。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClose(object sender, EventArgs e)
        {
            SimpleTcpClient conn = (SimpleTcpClient)sender;
            if (NCMap.TryGetKey(conn, out string UserName))
            {
                m_cmdSender.SendToAll(Helper.MessageWrapper(MessageType.SbLeave, UserName));
                if (conn == SpeakerConn && conn != HostConn)
                {
                    SpeakerConn = HostConn;
                    string HostName = NCMap.GetKey(HostConn);
                    HostConn.Send(Helper.MessageWrapper(MessageType.MicCapture, NCMap.GetKey(HostConn)));
                    m_cmdSender.SendToAll(Helper.MessageWrapper(MessageType.MicOwner, HostName));
                }
                NCMap.RemoveByKey(UserName);
                Config.RemoveUser(UserName);
                Config.RemoveAsker(UserName);
            }
            else
            {
                ErrorCount.Remove(conn);
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 偵測多餘的呼叫

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_audioReceiver.Dispose();
                    m_cmdSender.Dispose();
                    m_nameSender.Dispose();
                    m_trigger.Dispose();
                    if (SpeakerConn != null)
                    {
                        SpeakerConn.Dispose();
                        SpeakerConn = null;
                    }
                    if (HostConn != null)
                    {
                        HostConn.Dispose();
                        HostConn = null;
                    }
                    NCMap.Clear();
                    Config.ClearAskerList();
                    Config.ClearUserList();
                    ErrorCount.Clear();
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
