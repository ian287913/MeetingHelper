﻿using Controller.Message;
using Controller.NetWork;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

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
        private readonly Dictionary<IPAddress, RoomInfo> m_RoomList;
        /// <summary>
        /// 定時移除房間。
        /// </summary>
        private readonly Dictionary<IPAddress, DelayCaller> m_RemoveEvent;
        /// <summary>
        /// 房間的資訊，有名稱和IP位址。
        /// </summary>
        public List<RoomInfo> RoomList
        {
            get
            {
                List<RoomInfo> temp = new List<RoomInfo>();
                temp.AddRange(m_RoomList.Values);
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

        private bool isUpdateUL = false;
        #endregion

        #region Events
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
        /// <summary>
        /// 失去與Room的連線時觸發。
        /// </summary>
        public event EventHandler OnDisconnect;
        /// <summary>
        /// 使用者列表強制更新成功時觸發。
        /// </summary>
        public event EventHandler OnUpdate;
        #endregion

        #region Constructor
        public User()
        {
            m_RoomList = new Dictionary<IPAddress, RoomInfo>();
            m_RemoveEvent = new Dictionary<IPAddress, DelayCaller>();
            Config = new UserConfigure();
            RoomConfig = new RoomConfigure();
            m_audioSender = new SimpleUdpServer(0);
            m_Listener = new BroadcastListener(NetWorkPort.Broadcast);
            m_Listener.OnMessage += OnRoomFind;
        }
        #endregion

        #region Method
        /// <summary>
        /// 清除當前房間列表，並開始接收房間列表。
        /// </summary>
        public void StartListener()
        {
            foreach (var pair in m_RemoveEvent)
            {
                pair.Value.Close();
                m_RoomList.Remove(pair.Key);
            }
            m_RoomList.Clear();
            m_RemoveEvent.Clear();
            OnRoomListChanged?.Invoke(this, EventArgs.Empty);
            m_Listener.Start();
        }

        /// <summary>
        /// 保持當前房間列表，並停止接收房間列表。
        /// </summary>
        public void StopListener()
        {
            m_Listener.Close();
            foreach (var pair in m_RemoveEvent)
            {
                pair.Value.Close();
            }
            OnRoomListChanged?.Invoke(this, EventArgs.Empty);
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
            if (m_RoomList.ContainsKey(RoomAddr) && m_RemoveEvent.TryGetValue(RoomAddr, out DelayCaller caller))
            {
                caller.Cancel();
                Config.Name = Name;
                RoomConfig.Password = Password;
                if (m_cmdReceiver != null && !(m_cmdReceiver.IsConnected && m_cmdReceiver.Address.Equals(RoomAddr)))
                {
                    m_cmdReceiver.Dispose();
                    m_cmdReceiver = null;
                }
                if (m_cmdReceiver == null)
                {
                    m_cmdReceiver = new SimpleTcpClient(new IPEndPoint(RoomAddr, NetWorkPort.Commuting));
                    m_cmdReceiver.OnConnect += OnRoomConnect;
                    m_cmdReceiver.OnMessage += OnReceiveCommand;
                    m_cmdReceiver.OnClose += OnReceiverDisconnect;
                    m_cmdReceiver.OnError += OnReceiverError;
                    m_cmdReceiver.Connect();
                }
                else if (m_cmdReceiver.IsConnected)
                {
                    m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.JoinRoom, Config.Name, RoomConfig.Password));
                }
            }
            else
            {
                OnError?.Invoke(this, new ErrorEventArgs(new ArgumentException("Room does not exist.")));
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
        /// 返回發言權。
        /// </summary>
        public void BackMic()
        {
            if (Config.HaveMic && Config.IsInRoom)
            {
                m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.MicMissing));
            }
        }

        /// <summary>
        /// 強制刷新使用者列表。
        /// </summary>
        public void UpdateUserList()
        {
            if (m_cmdReceiver != null && m_cmdReceiver.IsConnected)
            {
                isUpdateUL = true;
                m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.UserList));
            }
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
            AddRoom(RoomAddr, new RoomInfo(RoomName, Name, Password != "", RoomAddr));
            JoinRoom(Name, Password, $"{RoomName}({RoomAddr})");
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
                if (IPAddress.TryParse(temp, out IPAddress addr)) return addr;
            }
            return IPAddress.Loopback;
        }

        /// <summary>
        /// 新增房間。
        /// </summary>
        /// <param name="RoomAddr">房間IP位址</param>
        /// <param name="RoomName">房間名稱</param>
        private void AddRoom(IPAddress RoomAddr, RoomInfo Info)
        {
            m_RoomList[RoomAddr] = Info;
            if (m_RemoveEvent.TryGetValue(RoomAddr, out DelayCaller caller))
            {
                caller.Reset();
            }
            else
            {
                m_RemoveEvent[RoomAddr] = new DelayCaller(500, () =>
                {
                    m_RoomList.Remove(RoomAddr);
                    m_RemoveEvent.Remove(RoomAddr);
                    OnRoomListChanged?.Invoke(this, EventArgs.Empty);
                });
                OnRoomListChanged?.Invoke(this, EventArgs.Empty);
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

        /// <summary>
        /// 清空外部綁定的事件
        /// </summary>
        public void RemoveAllEventHandlers()
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = GetType();
            EventInfo[] eventInfo = type.GetEvents(bindingFlags);
            foreach (EventInfo info in eventInfo)
            {
                if (type.GetField(info.Name, bindingFlags).GetValue(this) is Delegate del)
                {
                    foreach (Delegate invocation in del.GetInvocationList())
                    {
                        info.RemoveEventHandler(this, invocation);
                    }
                }
            }
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
            NameGetter.OnMessage += OnInfoReceive;
            NameGetter.OnConnect += (sender2, e2) => NameGetter.Send(e.Data);
            NameGetter.Connect();
        }

        /// <summary>
        /// 接收到房間名稱。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInfoReceive(object sender, TcpMessageEventArgs e)
        {
            SimpleTcpClient Getter = (SimpleTcpClient)sender;
            AddRoom(Getter.Address, new RoomInfo(e.Data, Getter.Address));
            Getter.Dispose();
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
        /// Receiver連線中斷。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceiverDisconnect(object sender, EventArgs e)
        {
            if (Config.IsInRoom)
            {
                OnDisconnect?.Invoke(this, EventArgs.Empty);
                Config = new UserConfigure();
                RoomConfig = new RoomConfigure();
            }
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
                    RoomInfo Info = m_RoomList[m_cmdReceiver.Address];
                    RoomConfig.SetByInfo(Info);
                    m_RoomList.Clear();
                    m_RemoveEvent.Clear();
                    m_cmdReceiver.Send(Helper.MessageWrapper(MessageType.UserList));
                    break;
                #endregion
                #region Forbidden
                case MessageType.Forbidden:
                    Config.IsInRoom = false;
                    Config.HaveMic = false;
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
                    if (isUpdateUL)
                    {
                        isUpdateUL = false;
                        RoomConfig.ClearUserList();
                        RoomConfig.AddUsers(cmd.Data);
                        OnUpdate?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        RoomConfig.AddUsers(cmd.Data);
                        OnEnterRoom?.Invoke(this, EventArgs.Empty);
                    }
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
                    foreach (var item in m_RemoveEvent)
                    {
                        item.Value.Close();
                    }
                    m_RemoveEvent.Clear();
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
