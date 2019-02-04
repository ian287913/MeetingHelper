using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Controller
{
    public static class NetWorkPort
    {
        /// <summary>
        /// 廣播用通訊埠。
        /// </summary>
        public static int Broadcast = 23090;
        /// <summary>
        /// 傳送指令用通訊埠。
        /// </summary>
        public static int Commuting = 12120;
        /// <summary>
        /// 傳送音訊用通訊埠。
        /// </summary>
        public static int Talking = 9120;
    }

    public class RoomConfigure
    {
        /// <summary>
        /// 使用者名稱的不重複列表。
        /// </summary>
        private HashSet<string> m_userlist;
        /// <summary>
        /// 要求者的不重複列表。
        /// </summary>
        private List<string> m_askerlist;
        /// <summary>
        /// 房間IP位址。
        /// </summary>
        public IPAddress Address { get; set; } = IPAddress.Loopback;
        /// <summary>
        /// 主席名稱。
        /// </summary>
        public string Host { get; set; } = "";
        /// <summary>
        /// 房間名稱。
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 房間密碼。
        /// </summary>
        public string Password { get; set; } = "";
        /// <summary>
        /// 建立時間。
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// 發言者名稱。
        /// </summary>
        public string Speaker { get; set; } = "";
        /// <summary>
        /// 房間成員名單。
        /// </summary>
        public string[] UserList
        {
            get
            {
                string[] temp = new string[m_userlist.Count];
                m_userlist.CopyTo(temp);
                return temp;
            }
        }
        /// <summary>
        /// 要求者名單，只有主席才有資料。
        /// </summary>
        public string[] AskerList
        {
            get
            {
                string[] temp = new string[m_askerlist.Count];
                m_askerlist.CopyTo(temp);
                return temp;
            }
        }

        public RoomConfigure()
        {
            m_userlist = new HashSet<string>();
            m_askerlist = new List<string>();
        }

        /// <summary>
        /// 新增使用者。
        /// </summary>
        /// <param name="UsersName">使用者名稱陣列</param>
        public void AddUsers(string[] UsersName)
        {
            m_userlist.UnionWith(UsersName);
        }
        /// <summary>
        /// 移除使用者。
        /// </summary>
        /// <param name="UsersName">使用者名稱陣列</param>
        public void RemoveUsers(string[] UsersName)
        {
            m_userlist.ExceptWith(UsersName);
        }
        /// <summary>
        /// 移除單一使用者。
        /// </summary>
        /// <param name="UsersName">使用者名稱</param>
        /// <returns>是否有移除</returns>
        public bool RemoveUser(string UsersName)
        {
            return m_userlist.Remove(UsersName);
        }
        /// <summary>
        /// 新增要求者。
        /// </summary>
        /// <param name="UserName"></param>
        public void AddAsker(string UserName)
        {
            m_askerlist.Remove(UserName);
            m_askerlist.Add(UserName);
        }
        /// <summary>
        /// 移除要求者。
        /// </summary>
        /// <param name="UserName">使用者名稱</param>
        /// <returns>是否有移除</returns>
        public bool RemoveAsker(string UserName)
        {
            return m_askerlist.Remove(UserName);
        }
        /// <summary>
        /// 是否包含指定的使用者名稱。
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public bool ContainsUser(string UserName)
        {
            return m_userlist.Contains(UserName);
        }
        /// <summary>
        /// 是否包含指定的要求者名稱。
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public bool ContainsAsker(string UserName)
        {
            return m_askerlist.Contains(UserName);
        }
        /// <summary>
        /// 清除使用者清單。
        /// </summary>
        public void ClearUserList()
        {
            m_userlist.Clear();
        }
        /// <summary>
        /// 清除要求者清單。
        /// </summary>
        public void ClearAskerList()
        {
            m_askerlist.Clear();
        }
        /// <summary>
        /// 取得建立<see cref="RoomInfo"/>的位元組資訊。
        /// </summary>
        /// <returns></returns>
        public byte[] GetInfoBytes()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
            byte[] hostBytes = Encoding.UTF8.GetBytes(Host);
            byte[] createAtBytes = BitConverter.GetBytes(CreatedAt.Ticks);
            byte[] result = new byte[createAtBytes.Length + nameBytes.Length + hostBytes.Length + 2];
            result.Initialize();
            createAtBytes.CopyTo(result, 0);
            result[8] = (byte)(Password != "" ? 1 : 0);
            nameBytes.CopyTo(result, createAtBytes.Length + 1);
            hostBytes.CopyTo(result, result.Length - hostBytes.Length);
            return result;
        }
        /// <summary>
        /// 用<see cref="RoomInfo"/>來輸入資訊。
        /// </summary>
        /// <param name="Info"></param>
        public void SetByInfo(RoomInfo Info)
        {
            Host = Info.Host;
            Address = Info.Address;
            Name = Info.Name;
            CreatedAt = Info.CreatedAt;
        }
    }

    public class UserConfigure
    {
        /// <summary>
        /// 使用者名稱。
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 使用者是否在房間內。
        /// </summary>
        public bool IsInRoom { get; internal set; } = false;
        /// <summary>
        /// 使用者是否有發言權。
        /// </summary>
        public bool HaveMic { get; internal set; } = false;
    }

    public class RoomInfo
    {
        public string Name { get; private set; }
        public string Host { get; private set; }
        public IPAddress Address { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool Locked { get; private set; }
        public RoomInfo(string Name, string Host, bool isLock, IPAddress addr)
        {
            this.Name = Name;
            this.Host = Host;
            Locked = isLock;
            CreatedAt = DateTime.UtcNow;
            Address = addr;
        }
        public RoomInfo(byte[] data, IPAddress addr)
        {
            long ticks = BitConverter.ToInt64(data, 0);
            CreatedAt = new DateTime(ticks, DateTimeKind.Utc);
            Locked = data[8] == 1;
            string[] temp = Encoding.UTF8.GetString(data, 9, data.Length - 9).Split('\0');
            Name = temp[0];
            Host = temp[1];
            Address = addr;
        }
        public byte[] ToBytes()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
            byte[] hostBytes = Encoding.UTF8.GetBytes(Host);
            byte[] createAtBytes = BitConverter.GetBytes(CreatedAt.Ticks);
            byte[] result = new byte[createAtBytes.Length + nameBytes.Length + hostBytes.Length + 2];
            result.Initialize();
            createAtBytes.CopyTo(result, 0);
            result[8] = (byte)(Locked ? 1 : 0);
            nameBytes.CopyTo(result, createAtBytes.Length + 1);
            hostBytes.CopyTo(result, result.Length - hostBytes.Length);
            return result;
        }
        public override string ToString()
        {
            return $"{Name }({Address})";
        }
    }
}
