using System;
using System.Net;

namespace Controller
{
    public delegate void TcpMessageEventHandler(object sender, TcpMessageEventArgs e);
    public class TcpMessageEventArgs : EventArgs
    {
        public int Length { get; private set; }
        public byte[] Data { get; private set; }
        public TcpMessageEventArgs(int dataLength, byte[] data)
        {
            Length = data.Length < dataLength ? data.Length : dataLength;
            Data = new byte[dataLength];
            Array.Copy(data, Data, Length);
        }
    }

    public delegate void UdpMessageEventHandler(object sender, UdpMessageEventArgs e);
    public class UdpMessageEventArgs : EventArgs
    {
        public IPEndPoint RemoteEndPoint { get; private set; }
        public int Length { get; private set; }
        public byte[] Data { get; private set; }
        public UdpMessageEventArgs(int dataLength, byte[] data, EndPoint endPoint)
        {
            Length = data.Length < dataLength ? data.Length : dataLength;
            Data = new byte[dataLength];
            Array.Copy(data, Data, Length);
            RemoteEndPoint = endPoint as IPEndPoint;
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }
        public ErrorEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
}