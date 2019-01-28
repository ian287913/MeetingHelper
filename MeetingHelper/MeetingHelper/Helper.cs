using Controller.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Controller
{
    public class Command
    {
        public MessageType Type { get; private set; }
        public string[] Data { get; private set; }
        public Command(byte[] data)
        {
            string temp = Encoding.UTF8.GetString(data, 5, data.Length - 5);
            Data = temp.Split('\0');
            Type = (MessageType)data[4];
        }
    }

    public static class Helper
    {
        public static IPEndPoint ChangePort(EndPoint ep, int port)
        {
            IPEndPoint iep = new IPEndPoint(new IPAddress(((IPEndPoint)ep).Address.GetAddressBytes()), port);
            return iep;
        }

        public static string GetString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static bool IsEnough(TcpMessageEventArgs e, out int RemainLength)
        {
            int len = BitConverter.ToInt32(e.Data, 0);
            RemainLength = len + 4 - e.Length;
            return RemainLength <= 0;
        }

        public static void ConcatBytes(ref byte[] data, ref ArraySegment<byte> data2)
        {
            data = data.Concat(data2.Take(data2.Count)).ToArray();
        }

        public static byte[] MessageWrapper(MessageType type, params object[] args)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)type);
                if (args.Length > 0)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(args[0].ToString());
                    ms.Write(bytes, 0, bytes.Length);
                    for (int i = 1; i < args.Length; i++)
                    {
                        bytes = Encoding.UTF8.GetBytes(args[i].ToString());
                        ms.WriteByte(0);
                        ms.Write(bytes, 0, bytes.Length);
                    }
                }
                data = BitConverter.GetBytes((int)ms.Length).Concat(ms.GetBuffer().Take((int)ms.Length)).ToArray();
            }
            return data;
        }
    }

    public class DelayCaller
    {
        private Stopwatch m_stopwatch;
        private long m_delayMS;
        private bool m_isCancel = false;
        private bool m_isFinish = false;
        private Action m_method;

        public DelayCaller(int millisecondsDelay, Action action)
        {
            m_stopwatch = new Stopwatch();
            if (action == null) return;
            m_delayMS = millisecondsDelay;
            m_method = action;
            Task.Factory.StartNew(WaitToCall);
        }

        public void Reset()
        {
            m_stopwatch.Restart();
        }

        public void Reset(int millisecondsDelay)
        {
            m_stopwatch.Reset();
            m_delayMS = millisecondsDelay;
            m_stopwatch.Start();
        }

        public void Cancel()
        {
            m_isCancel = true;
        }

        public void Restart()
        {
            if (m_isFinish)
            {
                Task.Factory.StartNew(WaitToCall);
            }
            else
            {
                Reset();
            }
        }

        private void WaitToCall()
        {
            m_isFinish = false;
            m_stopwatch.Restart();
            while (m_stopwatch.ElapsedMilliseconds < m_delayMS) {/* Wait */}
            m_stopwatch.Stop();
            if (!m_isCancel)
                m_method.Invoke();
            m_isFinish = true;
        }

        internal void Close()
        {
            m_stopwatch.Stop();
            m_method = null;
            m_isCancel = true;
            m_delayMS = 0;
            m_isFinish = false;
        }
    }

    public enum TimeUnit : long
    {
        Second = 1000,
        Minute = 60000,
        Hour = 3600000
    }

    public class Map<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> m_K2V;
        private readonly Dictionary<TValue, TKey> m_V2K;

        public TKey[] Keys => m_K2V.Keys.ToArray();
        public TValue[] Values => m_V2K.Keys.ToArray();
        public TValue this[TKey key]
        {
            get
            {
                return m_K2V[key];
            }
            set
            {
                AddByKey(key, value);
            }
        }

        public Map()
        {
            m_K2V = new Dictionary<TKey, TValue>();
            m_V2K = new Dictionary<TValue, TKey>();
        }

        public void AddByKey(TKey key, TValue value)
        {
            if (m_K2V.TryGetValue(key, out TValue val))
            {
                m_V2K.Remove(val);
            }
            m_K2V[key] = value;
            m_V2K[value] = key;
        }
        public void AddByValue(TKey key, TValue value)
        {
            if (m_V2K.TryGetValue(value, out TKey _key))
            {
                m_K2V.Remove(_key);
            }
            m_K2V[key] = value;
            m_V2K[value] = key;
        }

        public void RemoveByKey(TKey key)
        {
            if (m_K2V.TryGetValue(key, out TValue val))
            {
                m_V2K.Remove(val);
            }
            m_K2V.Remove(key);
        }
        public void RemoveByValue(TValue value)
        {
            if (m_V2K.TryGetValue(value, out TKey key))
            {
                m_K2V.Remove(key);
            }
            m_V2K.Remove(value);
        }

        public TValue GetValue(TKey key)
        {
            if (m_K2V.TryGetValue(key, out TValue value))
            {
                return value;
            }
            return default(TValue);
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_K2V.TryGetValue(key, out value);
        }

        public TKey GetKey(TValue value)
        {
            if (m_V2K.TryGetValue(value, out TKey key))
            {
                return key;
            }
            return default(TKey);
        }
        public bool TryGetKey(TValue value, out TKey key)
        {
            return m_V2K.TryGetValue(value, out key);
        }

        public bool ContainsKey(TKey key)
        {
            return m_K2V.ContainsKey(key);
        }
        public bool ContainsValue(TValue value)
        {
            return m_V2K.ContainsKey(value);
        }

        public void Clear()
        {
            m_K2V.Clear();
            m_V2K.Clear();
        }
    }
}
