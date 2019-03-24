using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Controller.NetWork;
using System.Linq;
using System.Net;

namespace Controller
{
    /// <summary>
    /// 用於主席收集所有人的簽名(錄音)
    /// </summary>
    public class AttendenceSheet
    {
        #region vars
        string roomName;
        SimpleTcpServer server;
        public List<Attendant> signedList { get; private set; }
        #endregion

        #region constructors
        public AttendenceSheet(string name)
        {
            roomName = name + "_" + string.Format("{0:d}", DateTime.Now);
            server = new SimpleTcpServer(NetWorkPort.Signing);
            server.OnMessage += SignIn;
            signedList = new List<Attendant>();
            signedList.Clear();
            StartSigning();
        }
        #endregion

        #region Collect Signs

        /// <summary>
        /// 等待來簽名的人:)
        /// </summary>
        public void StartSigning()
        {
            server.Start();
        }
        /// <summary>
        /// 停止簽到
        /// </summary>
        public void StopSigning()
        {
            server.Stop();
        }



        /// <summary>
        /// 得到要簽名的人(IP)與音訊
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="e"></param>
        private void SignIn(object conn, TcpMessageEventArgs e)
        {
            SimpleTcpClient client = (SimpleTcpClient)conn;
            int nameL = BitConverter.ToInt32(e.Data,0);
            string clientName = BitConverter.ToString(e.Data, 4, nameL);
            Attendant signedUser = new Attendant(client, clientName);
            string filename = client.Address.ToString();

            CreateFile(filename + ".wav", e.Data); //音訊存檔
            signedList.Add(signedUser); //已簽到
        }
        #endregion

        #region Events
        /// <summary>
        /// 在進入房間時存取名字((暫無功能))
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="e"></param>
        private void OnAccept(object conn, EventArgs e)
        {
            SimpleTcpClient client = (SimpleTcpClient)conn;
        }
        #endregion

        #region helpers
        /// <summary>
        /// 存檔用
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private void CreateFile(string fileName, byte[] data)
        {
            string filePath = Android.OS.Environment.ExternalStorageDirectory.Path;
            DirectoryInfo file = new DirectoryInfo(filePath + "/AttendenceSheet/" + roomName);
            if (!file.Exists) file.Create();
            System.IO.File.WriteAllBytes(file.FullName + "/" + fileName, data);
        }
        #endregion
    }

    public class Attendant
    {
        #region vars
        public string name { get; private set; }
        SimpleTcpClient client;
        #endregion

        #region constructors
        public Attendant(string attName, IPEndPoint remoteIPE)
        {
            name = attName;
            client = new SimpleTcpClient(remoteIPE);
            client.Connect();
        }
        public Attendant(SimpleTcpClient c, string att_name)
        {
            name = att_name;
            client = c;
        }
        #endregion

        #region Sign
        public void Sign(byte[] audioData)
        {
            client.Send(packageWarp(audioData));
        }

        public void Close()
        {
            client.Close();
        }
        #endregion

        #region Helpers
        private byte[] packageWarp(byte[] audioData)
        {
            byte[] output;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)name.Length);
                byte[] bytes = Encoding.UTF8.GetBytes(name);
                ms.Write(bytes, 0, bytes.Length);
                ms.Write(audioData, 0, audioData.Length);
                output = BitConverter.GetBytes((int)ms.Length).Concat(ms.GetBuffer().Take((int)ms.Length)).ToArray();
            }
            
            return output;
        }
        #endregion
    }
}
