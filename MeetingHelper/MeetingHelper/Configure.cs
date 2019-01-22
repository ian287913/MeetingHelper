using System.Collections.Generic;
using System.Net;
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
        private HashSet<string> m_userlist;
        private List<string> m_askerlist;
        /// <summary>
        /// 房間IP位址。
        /// </summary>
        public IPAddress Address { get; set; }
        /// <summary>
        /// 主席名稱。
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// 房間名稱。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 房間密碼。
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 發言者名稱。
        /// </summary>
        public string Speaker { get; set; }
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
                m_userlist.CopyTo(temp);
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
}
