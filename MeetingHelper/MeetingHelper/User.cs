using Controller.Message;
using Controller.NetWork;
using System;
using System.Collections.Generic;
using System.Net;
namespace Controller.Component
{
    public class UserEventArgs : EventArgs
    {
        public string Name { get; private set; }
        public UserEventArgs(string UserName)
        {
            Name = UserName;
        }
    }

    public delegate void UserEventHandler(object sender, UserEventArgs e);

    public class WrongPasswordEventArgs : EventArgs
    {
        public int RemainingTimes { get; private set; }
        public WrongPasswordEventArgs(int rtimes)
        {
            RemainingTimes = rtimes;
        }
    }

    public delegate void WrongPasswordHandler(object sender, WrongPasswordEventArgs e);

    public class User : IDisposable
    {
        #region Variable
        /// <summary>
        /// 接收廣播訊號。
        /// </summary>
        private readonly BroadcastListener m_Listener;
        /// <summary>
        /// 接收命令。
        /// </summary>
        private SimpleTcpClient m_cmdReceiver;
        /// <summary>
        /// 傳送音訊。
        /// </summary>
        private SimpleUdpServer m_audioSender;
        /// <summary>
        /// 房間列表。
        /// </summary>
        private readonly Dictionary<IPAddress, string> m_RoomList;
        /// <summary>
        /// 定時移除房間。
        /// </summary>
        private readonly Dictionary<IPAddress, DelayCaller> RemoveEvent;
        /// <summary>
        /// 房間的資訊，有名稱和IP位址。
        /// </summary>
        public List<string> RoomList
        {
            get
            {
                List<string> temp = new List<string>();
                foreach (var roomInfo in m_RoomList)
                {
                    temp.Add($"{roomInfo.Value}({roomInfo.Key})");
                }
                return temp;
            }
        }
        /// <summary>
        /// 使用者資訊。
        /// </summary>
        public UserConfigure Config { get; private set; }
        /// <summary>
        /// 目前加入的房間資訊。
        /// </summary>
        public RoomConfigure RoomConfig { get; private set; }
        #endregion

        #region Event
        /// <summary>
        /// 房間列表變更時觸發。
        /// </summary>
        public event EventHandler OnRoomListChanged;
        /// <summary>
        /// 當密碼錯誤時觸發。
        /// </summary>
        public event WrongPasswordHandler OnWrongPassword;
        /// <summary>
        /// 當名稱重複時觸發。
        /// </summary>
        public event EventHandler OnDuplicateName;
        /// <summary>
        /// 成功進入房間並取得使用者列表時觸發。
        /// </summary>
        public event EventHandler OnEnterRoom;
        /// <summary>
        /// 獲得發言權時觸發。
        /// </summary>
        public event EventHandler OnMicCapture;
        /// <summary>
        /// 被取回發言權時觸發。
        /// </summary>
        public event EventHandler OnMicMissing;
        /// <summary>
        /// 主席拒絕要求時觸發。
        /// </summary>
        public event EventHandler OnHostRefused;
        /// <summary>
        /// 有人要求發言時觸發。
        /// </summary>
        public event UserEventHandler OnRequest;
        /// <summary>
        /// 有新的使用者加入時觸發。
        /// </summary>
        public event UserEventHandler OnUserJoin;
        /// <summary>
        /// 有使用者離開時觸發。
        /// </summary>
        public event UserEventHandler OnUserExit;
        /// <summary>
        /// 發言者變更時觸發。
        /// </summary>
        public event UserEventHandler OnSpeakerChanged;
        /// <summary>
        /// 有錯誤發生時觸發。
        /// </summary>
        public event ErrorEventHandler OnError;
        /// <summary>
        /// 發送了無權限的要求時觸發，需要重新加入房間。
        /// </summary>
        public event EventHandler OnForbid;
        #endregion

        #region Constructor
        public User()
        {
            m_RoomList = new Dictionary<IPAddress, string>();
            RemoveEvent = new Dictionary<IPAddress, DelayCaller>();
            Config = new UserConfigure();
            RoomConfig = new RoomConfigure();
            m_audioSender = new SimpleUdpServer(0);
            m_Listener = new BroadcastListener(NetWorkPort.Broadcast);
            m_Listener.OnMessage += OnRoomFind;
        }
        #endregion

        #region Method
        /// <summary>
        /// 開始接收房間列表。
        /// </summary>
        public void StartListener()
        {
            m_Listener.Start();
        }

        /// <summary>
        /// 取消接收房間列表。
        /// </summary>
        public void StopListener()
        {
            m_Listener.Close();
        }

        /// <summary>
        /// 加入房間。
        /// </summary>
        /// <param name="Name">使用者名稱</param>
        /// <param name="Password">房間密碼</param>
        /// <param name="RoomInfo">房間資訊，為<see cref="RoomList"/>的內容。</param>
        public void JoinRoom(string Name, string Password, string RoomInfo)
        {
            IPAddress RoomAddr = FindRoom(RoomInfo);
            Config.Name = Name;
            RoomConfig.Password = Password;
            if (m_cmdReceiver != null && !m_cmdReceiver.Address.Equals(RoomAddr))
            {
                m_cmdReceiver.Dispose();
                m_cmdReceiver = null;
            }
            if (m_cmdReceiver is null)
            {
                m_cmdReceiver = new SimpleTcpClient(new IPEndPoint(RoomAddr, NetWorkPort.Commuting));
                m_cmdReceiver.OnConnect += OnRoomConnect;
                m_cmdReceiver.OnMessage += OnReceiveCommand;
                m_cmdReceiver.OnError += OnReceiverError;
                m_cmdReceiver.Connect();
            }
            else if (m_cmdReceiver.IsConnected)
            {
                m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.JoinRoom, Config.Name, RoomConfig.Password));
            }
            else
            {
                OnError?.Invoke(this, new ErrorEventArgs(new Exception("Can't Join Room. Wait Later.")));
            }
        }

        /// <summary>
        /// 離開房間。
        /// </summary>
        public void ExitRoom()
        {
            if (Config.IsInRoom)
            {
                Config = new UserConfigure();
                RoomConfig = new RoomConfigure();
            }
            m_cmdReceiver.Close();
        }

        /// <summary>
        /// 傳送音訊。
        /// </summary>
        /// <param name="data">音訊資料</param>
        public void SendAudio(byte[] data)
        {
            if (Config.HaveMic && Config.IsInRoom)
            {
                m_audioSender.Send(data, new IPEndPoint(RoomConfig.Address, NetWorkPort.Talking));
            }
        }

        /// <summary>
        /// 要求發言權。
        /// </summary>
        public void WantMic()
        {
            if (!Config.HaveMic && Config.IsInRoom)
            {
                m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.MicCapture, Config.Name));
            }
        }

        /// <summary>
        /// 用房間資訊搜尋IP位址。
        /// </summary>
        /// <param name="RoomInfo">房間資訊</param>
        /// <returns>房間IP位址</returns>
        private IPAddress FindRoom(string RoomInfo)
        {
            int id = RoomInfo.LastIndexOf('(');
            if (id != -1)
            {
                string temp = RoomInfo.Substring(id + 1).Replace(")", "");
                try
                {
                    return IPAddress.Parse(temp);
                }
                catch { }
            }
            return IPAddress.Loopback;
        }

        /// <summary>
        /// 成為主席。
        /// </summary>
        /// <param name="RoomName">房間名稱</param>
        /// <param name="Password">房間密碼</param>
        /// <param name="Name">使用者名稱</param>
        /// <param name="RoomAddr">房間IP位址</param>
        public void BecomeHost(string RoomName, string Password, string Name, IPAddress RoomAddr)
        {
            AddRoom(RoomAddr, RoomName);
            JoinRoom(Name, Password, $"{RoomName}({RoomAddr})");
        }

        /// <summary>
        /// 新增房間。
        /// </summary>
        /// <param name="RoomAddr">房間IP位址</param>
        /// <param name="RoomName">房間名稱</param>
        private void AddRoom(IPAddress RoomAddr, string RoomName)
        {
            m_RoomList[RoomAddr] = RoomName;
            if (RemoveEvent.TryGetValue(RoomAddr, out DelayCaller caller))
            {
                caller.Reset();
            }
            else
            {
                RemoveEvent[RoomAddr] = new DelayCaller(5000, () =>
                {
                    m_RoomList.Remove(RoomAddr);
                    RemoveEvent.Remove(RoomAddr);
                    OnRoomListChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }

        /// <summary>
        /// 允許使用者發言。
        /// </summary>
        /// <param name="UserName"></param>
        public void AcceptAsker(string UserName)
        {
            m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.Accept, UserName));
        }

        /// <summary>
        /// 拒絕使用者發言。
        /// </summary>
        /// <param name="UserName"></param>
        public void RefuseAsker(string UserName)
        {
            RoomConfig.RemoveAsker(UserName);
            m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.Refuse, UserName));
        }
        #endregion

        #region EventCaller
        /// <summary>
        /// 搜尋到房間。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRoomFind(object sender, UdpMessageEventArgs e)
        {
            SimpleTcpClient NameGetter = new SimpleTcpClient(Helper.ChangePort(e.RemoteEndPoint, NetWorkPort.Broadcast));
            NameGetter.OnMessage += OnNameReceive;
            NameGetter.OnConnect += (sender2, e2) =>
            {
                NameGetter.Send(e.Data);
            };
            NameGetter.Connect();
        }
        /// <summary>
        /// 接收到房間名稱。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNameReceive(object sender, TcpMessageEventArgs e)
        {
            SimpleTcpClient Getter = (SimpleTcpClient)sender;
            AddRoom(Getter.Address, Helper.GetString(e.Data));
            Getter.Dispose();
            OnRoomListChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// 成功連線到房間主機，並要求進入房間。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRoomConnect(object sender, EventArgs e)
        {
            m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.JoinRoom, Config.Name, RoomConfig.Password));
        }
        /// <summary>
        /// TCP連線出現問題。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceiverError(object sender, ErrorEventArgs e)
        {
            OnError?.Invoke(this, e);
        }
        /// <summary>
        /// 接收到房間主機下的指令。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceiveCommand(object sender, TcpMessageEventArgs e)
        {
            byte[] TotalData = e.Data;
            if (!Helper.IsEnough(e, out int remain))
            {
                var RemainData = m_cmdReceiver.ReceiveBytes(remain);
                Helper.ConcatBytes(ref TotalData, ref RemainData);
            }
            Command cmd = new Command(TotalData);
            switch (cmd.Type)
            {
                #region Success
                case MessageType.Success:
                    Config.IsInRoom = true;
                    RoomConfig.Host = cmd.Data[0];
                    RoomConfig.Name = m_RoomList[m_cmdReceiver.Address];
                    RoomConfig.Address = m_cmdReceiver.Address;
                    m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.UserList));
                    break;
                #endregion
                #region Forbidden
                case MessageType.Forbidden:
                    Config.IsInRoom = false;
                    m_cmdReceiver.Close();
                    OnForbid?.Invoke(this, EventArgs.Empty);
                    break;
                #endregion
                #region Unauthorized
                case MessageType.Unauthorized:
                    OnWrongPassword?.Invoke(this, new WrongPasswordEventArgs(Convert.ToInt32(cmd.Data[0])));
                    break;
                #endregion
                #region	Conflict
                case MessageType.Conflict:
                    OnDuplicateName?.Invoke(this, EventArgs.Empty);
                    break;
                #endregion
                #region SbJoin
                case MessageType.SbJoin:
                    RoomConfig.AddUsers(cmd.Data);
                    OnUserJoin?.Invoke(this, new UserEventArgs(cmd.Data[0]));
                    break;
                #endregion
                #region SbLeave
                case MessageType.SbLeave:
                    RoomConfig.RemoveUsers(cmd.Data);
                    OnUserExit?.Invoke(this, new UserEventArgs(cmd.Data[0]));
                    break;
                #endregion
                #region UserList
                case MessageType.UserList:
                    RoomConfig.AddUsers(cmd.Data);
                    OnEnterRoom?.Invoke(this, EventArgs.Empty);
                    break;
                #endregion
                #region MicCapture
                case MessageType.MicCapture:
                    Config.HaveMic = true;
                    OnMicCapture?.Invoke(this, EventArgs.Empty);
                    break;
                #endregion
                #region MicMissing
                case MessageType.MicMissing:
                    Config.HaveMic = false;
                    OnMicMissing?.Invoke(this, EventArgs.Empty);
                    break;
                #endregion
                #region MicOwner
                case MessageType.MicOwner:
                    RoomConfig.Speaker = cmd.Data[0];
                    RoomConfig.RemoveAsker(cmd.Data[0]);
                    OnSpeakerChanged?.Invoke(this, new UserEventArgs(cmd.Data[0]));
                    break;
                #endregion
                #region Refuse
                case MessageType.Refuse:
                    OnHostRefused?.Invoke(this, EventArgs.Empty);
                    break;
                #endregion
                #region Request
                case MessageType.Request:
                    RoomConfig.AddAsker(cmd.Data[0]);
                    OnRequest?.Invoke(this, new UserEventArgs(cmd.Data[0]));
                    break;
                #endregion
                default:
                    break;
            }
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
                    m_Listener.Dispose();
                    m_cmdReceiver.Dispose();
                    m_audioSender.Dispose();
                    m_RoomList.Clear();
                    foreach (var item in RemoveEvent)
                    {
                        item.Value.Close();
                    }
                    RemoveEvent.Clear();
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
