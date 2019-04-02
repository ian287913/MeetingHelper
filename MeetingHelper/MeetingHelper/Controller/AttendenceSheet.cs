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
        bool isSigning;
        Dictionary<string,byte[]> signDataList;
        #endregion

        #region constructors
        public AttendenceSheet(string name)
        {
            isSigning = false;
            roomName = name + "_" + DateTime.Now.Date.ToString("yyyy-MM-dd");
            server = new SimpleTcpServer(NetWorkPort.Signing);
            server.OnMessage += SignIn;
            server.OnAccept += OnAccept;
            signedList = new List<Attendant>();
            signedList.Clear();
            signDataList = new Dictionary<string, byte[]>();
            StartSigning();
        }
        ~AttendenceSheet()
        {
            StopSigning();
            signDataList.Clear();
        }
        #endregion

        #region Collect Signs

        /// <summary>
        /// 等待來簽名的人:)
        /// </summary>
        public void StartSigning()
        {
            server.Start();
            isSigning = true;
        }
        /// <summary>
        /// 停止簽到
        /// </summary>
        public void StopSigning()
        {
            server.Stop();
            isSigning = false;
        }
        
        /// <summary>
        /// 得到要簽名的人(IP)與音訊
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="e"></param>
        private void SignIn(object conn, TcpMessageEventArgs e)
        {
            SimpleTcpClient client = (SimpleTcpClient)conn;
            int nameL = BitConverter.ToInt32(e.Data,4);
            bool isEnd = Convert.ToBoolean(e.Data[8]);
            string clientName = Encoding.UTF8.GetString(e.Data, 9, nameL);
            Attendant signedUser = new Attendant(client, clientName);
            string filename = client.Address.ToString();

            byte[] data = new byte[e.Data.Length - 9 - nameL];
            Array.Copy(e.Data, 9 + nameL, data, 0, e.Data.Length - 9 - nameL);
            CreateFile(filename + ".wav", data); //音訊存檔
            if(isEnd)
                signedList.Add(signedUser); //已簽到
        }
        #endregion

        #region Events
        /// <summary>
        /// 在進入房間時KEEP ALIVE
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="e"></param>
        private void OnAccept(object conn, EventArgs e)
        {
            SimpleTcpClient client = (SimpleTcpClient)conn;
            server.KeepAlive(client);
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
            DirectoryInfo file = new DirectoryInfo(filePath + "/AttendenceSheets/" + roomName);
            if (!file.Exists) file.Create();
            byte[] wavHeader;
            MakeHeader(data.LongLength, out wavHeader);
            byte[] newb = new byte[44 + data.Length];
            Buffer.BlockCopy(wavHeader, 0, newb, 0, 44);
            Buffer.BlockCopy(data, 0, newb, 44, data.Length);
            System.IO.File.WriteAllBytes(file.FullName + "/" + fileName, newb);
        }
        private void CollectData(string userName, byte[] data, bool isEnd)
        {
            if (!signDataList.ContainsKey(userName))
            {
                signDataList.Add(userName, data);
            }
            else
            {
                byte[] newb = new byte[signDataList[userName].Length + data.Length];
                Buffer.BlockCopy(signDataList[userName], 0, newb, 0, signDataList[userName].Length);
                Buffer.BlockCopy(data, 0, newb, signDataList[userName].Length, data.Length);
                signDataList[userName] = newb;
            }
            if (isEnd)
            {
                CreateFile(userName, signDataList[userName]);
            }
        }
        private void MakeHeader(long audioLen, out byte[] header)
        {
            long totalDataLen = audioLen + 36;
            header = new byte[44];
            header[0] = (byte)'R'; // RIFF/WAVE header
            header[1] = (byte)'I';
            header[2] = (byte)'F';
            header[3] = (byte)'F';
            header[4] = (byte)(totalDataLen & 0xff);
            header[5] = (byte)((totalDataLen >> 8) & 0xff);
            header[6] = (byte)((totalDataLen >> 16) & 0xff);
            header[7] = (byte)((totalDataLen >> 24) & 0xff);
            header[8] = (byte)'W';
            header[9] = (byte)'A';
            header[10] = (byte)'V';
            header[11] = (byte)'E';
            header[12] = (byte)'f'; // 'fmt ' chunk
            header[13] = (byte)'m';
            header[14] = (byte)'t';
            header[15] = (byte)' ';
            header[16] = 16; // 4 bytes: size of 'fmt ' chunk
            header[17] = 0;
            header[18] = 0;
            header[19] = 0;
            header[20] = 1; // format = 1
            header[21] = 0;
            header[22] = 1;
            header[23] = 0;
            header[24] = 128;
            header[25] = 62;
            header[26] = 0;
            header[27] = 0;
            header[28] = 0;
            header[29] = 125;
            header[30] = 0;
            header[31] = 0;
            header[32] = 2; // block align
            header[33] = 0;
            header[34] = 16; // bits per sample
            header[35] = 0;
            header[36] = (byte)'d';
            header[37] = (byte)'a';
            header[38] = (byte)'t';
            header[39] = (byte)'a';
            header[40] = (byte)(audioLen & 0xff);
            header[41] = (byte)((audioLen >> 8) & 0xff);
            header[42] = (byte)((audioLen >> 16) & 0xff);
            header[43] = (byte)((audioLen >> 24) & 0xff);
        }
        #endregion
    }

    public class Attendant
    {
        #region vars
        public string name { get; private set; }
        SimpleTcpClient client;
        int bufferSize = 1000;
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
        public async void Sign(byte[] audioData)
        {
            await System.Threading.Tasks.Task.Factory.StartNew(()=>DivSend(audioData));
        }

        public void Close()
        {
            client.Close();
        }
        #endregion

        #region Helpers
        void DivSend(byte[] data)
        {
            int index = 0;
            while (index < data.Length)
            {
                if (data.Length - index > bufferSize)
                {
                    byte[] subarr = new byte[bufferSize];
                    Array.Copy(data, index, subarr, 0, bufferSize);
                    client.Send(packageWarp(subarr, false));
                    index += bufferSize;
                }
                else
                {
                    byte[] subarr = new byte[data.Length - index];
                    Array.Copy(data, index, subarr, 0, data.Length - index);
                    client.Send(packageWarp(subarr, true));
                    index = data.Length;
                    return;
                }
                System.Threading.Thread.Sleep(50);
            }
        }
        private byte[] packageWarp(byte[] audioData, bool isEnd)
        {
            byte[] output;
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(name);
                ms.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
                ms.WriteByte(Convert.ToByte(isEnd));
                ms.Write(bytes, 0, bytes.Length);
                ms.Write(audioData, 0, audioData.Length);
                output = BitConverter.GetBytes((int)ms.Length).Concat(ms.GetBuffer().Take((int)ms.Length)).ToArray();
            }
            
            return output;
        }
        #endregion
    }
}
