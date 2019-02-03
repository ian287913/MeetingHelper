using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DetailedState = Android.Net.NetworkInfo.DetailedState;
namespace Controller.Component
{
    public enum WifiDetailedState
    {
        ///<summary>空閒</summary>
        Idle,
        ///<summary>正在掃描</summary>
        Scanning,
        ///<summary>連接中</summary>
        Connecting,
        ///<summary>正在進行身份驗證</summary>
        Authenticating,
        ///<summary>正在獲取IP地址</summary>
        ObtainingIpaddr,
        ///<summary>已連接</summary>
        Connected,
        ///<summary>已暫停</summary>
        Suspended,
        ///<summary>正在斷開連接</summary>
        Disconnecting,
        ///<summary>已斷開</summary>
        Disconnected,
        ///<summary>失敗</summary>
        Failed,
        ///<summary>已阻止</summary>
        Blocked,
        ///<summary>暫時關閉（網絡狀況不佳）</summary>
        VerifyingPoorLink,
        ///<summary>判斷是否需要瀏覽器二次登錄</summary>
        CaptivePortalCheck,
    }
    public class NetworkChangedEventArgs : EventArgs
    {
        public WifiDetailedState State { get; private set; }
        public NetworkChangedEventArgs(DetailedState detailedState)
        {
            if (detailedState == DetailedState.Authenticating)
            {
                State = WifiDetailedState.Authenticating;
            }
            else if (detailedState == DetailedState.Blocked)
            {
                State = WifiDetailedState.Blocked;
            }
            else if (detailedState == DetailedState.CaptivePortalCheck)
            {
                State = WifiDetailedState.CaptivePortalCheck;
            }
            else if (detailedState == DetailedState.Connected)
            {
                State = WifiDetailedState.Connected;
            }
            else if (detailedState == DetailedState.Connecting)
            {
                State = WifiDetailedState.Connecting;
            }
            else if (detailedState == DetailedState.Disconnected)
            {
                State = WifiDetailedState.Disconnected;
            }
            else if (detailedState == DetailedState.Disconnecting)
            {
                State = WifiDetailedState.Disconnecting;
            }
            else if (detailedState == DetailedState.Failed)
            {
                State = WifiDetailedState.Failed;
            }
            else if (detailedState == DetailedState.Idle)
            {
                State = WifiDetailedState.Idle;
            }
            else if (detailedState == DetailedState.ObtainingIpaddr)
            {
                State = WifiDetailedState.ObtainingIpaddr;
            }
            else if (detailedState == DetailedState.Scanning)
            {
                State = WifiDetailedState.Scanning;
            }
            else if (detailedState == DetailedState.Suspended)
            {
                State = WifiDetailedState.Suspended;
            }
            else if (detailedState == DetailedState.VerifyingPoorLink)
            {
                State = WifiDetailedState.VerifyingPoorLink;
            }
        }
    }

    public delegate void ExceptionEventHandler(string message);
    public delegate void NetworkChangedEventHandler(object sender, NetworkChangedEventArgs e);
    public class WifiController : IDisposable
    {
        private static WifiManager manager = null;
        private static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        private readonly WifiReceiverStatus wifiStatus = new WifiReceiverStatus();
        private readonly WifiReceiver wifiReceiver = new WifiReceiver();
        private readonly Context ctx = null;
        public static event ExceptionEventHandler OnException;
        public event NetworkChangedEventHandler OnNetworkChanged;
        public void ClearEvents()
        {
            OnNetworkChanged = null;
            OnException = null;
        }
        public NetworkChangedEventArgs currentStatus = new NetworkChangedEventArgs(DetailedState.Idle);
        public bool Enable
        {
            get
            {
                return manager.IsWifiEnabled;
            }
            set
            {
                if (manager.IsWifiEnabled != value) manager.SetWifiEnabled(value);
            }
        }
        public WifiInfo ConnectionInfo => manager.ConnectionInfo;
        public WifiController() : this(Android.App.Application.Context) { }
        public WifiController(Context context)
        {
            ctx = context;
            try
            {
                manager = (WifiManager)ctx.GetSystemService(Context.WifiService);
                if (!manager.IsWifiEnabled)
                    manager.SetWifiEnabled(true);
                ctx.RegisterReceiver(wifiStatus, new IntentFilter(WifiManager.NetworkStateChangedAction));
                ctx.RegisterReceiver(wifiReceiver, new IntentFilter(WifiManager.ScanResultsAvailableAction));
                wifiStatus.OnDataReceive = OnDataReceive;
            }
            catch (Exception e)
            {
                OnException?.Invoke("[Wifi Init Failed] : " + e.Message);
            }
        }
        private void OnDataReceive(Context context, Intent intent)
        {
            NetworkInfo info = (NetworkInfo)intent.GetParcelableExtra(WifiManager.ExtraNetworkInfo);
            DetailedState state = info.GetDetailedState();
            currentStatus = new NetworkChangedEventArgs(state);
            OnNetworkChanged?.Invoke(this, currentStatus);
        }
        public async Task<IList<ScanResult>> GetWifiList()
        {
            autoResetEvent.Reset();
            if (manager.WifiState == WifiState.Enabling)
            {
                while (manager.WifiState == WifiState.Enabling) ;
            }
            manager.StartScan();
            await Task.Run(() => autoResetEvent.WaitOne());
            return manager.ScanResults;
        }
        public void ConnectNetWork(string Ssid, string Password)
        {
            try
            {
                var ConfigurationList = manager.ConfiguredNetworks;
                var info = manager.ConnectionInfo;
                WifiConfiguration conf = null;
                int networkId = -1;
                if (isFound(Ssid, out int index))
                {
                    ScanResult result = manager.ScanResults[index];
                    string cap = result.Capabilities.ToLower();
                    if (cap.Contains("wpa"))
                    {
                        conf = CreateWifiConfiguration(Ssid, Password, 3);
                    }
                    else if (cap.Contains("wep"))
                    {
                        conf = CreateWifiConfiguration(Ssid, Password, 2);
                    }
                    else
                    {
                        conf = CreateWifiConfiguration(Ssid, Password, 1);
                    }
                    networkId = manager.AddNetwork(conf);
                    if (networkId != -1)
                    {
                        manager.Disconnect();
                        manager.EnableNetwork(networkId, true);
                        manager.Reconnect();
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke("[Wifi Connect Failed] : " + e.Message);
            }
        }
        public int CalculateLevel(string Ssid)
        {
            if (isFound(Ssid, out int index))
            {
                return WifiManager.CalculateSignalLevel(manager.ScanResults[index].Level, 10);
            }
            else
            {
                return 0;
            }
        }
        private bool isFound(string Ssid, out int index)
        {
            index = -1;
            for (int i = 0; i < manager.ScanResults.Count; i++)
            {
                if (manager.ScanResults[i].Ssid == Ssid)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        private WifiConfiguration CreateWifiConfiguration(string Ssid, string Password, int type)
        {
            WifiConfiguration conf = new WifiConfiguration();
            conf.AllowedAuthAlgorithms.Clear();
            conf.AllowedGroupCiphers.Clear();
            conf.AllowedKeyManagement.Clear();
            conf.AllowedPairwiseCiphers.Clear();
            conf.AllowedProtocols.Clear();
            conf.Ssid = $"\"{Ssid}\"";
            if (type == 1)//NONE
            {
                conf.AllowedKeyManagement.Set((int)KeyManagementType.None);
            }
            else if (type == 2)//WEP
            {
                conf.HiddenSSID = true;
                conf.WepKeys[0] = $"\"{Password}\"";
                conf.AllowedAuthAlgorithms.Set((int)AuthAlgorithmType.Shared);
                conf.AllowedGroupCiphers.Set((int)GroupCipherType.Ccmp);
                conf.AllowedGroupCiphers.Set((int)GroupCipherType.Tkip);
                conf.AllowedGroupCiphers.Set((int)GroupCipherType.Wep40);
                conf.AllowedGroupCiphers.Set((int)GroupCipherType.Wep104);
                conf.AllowedKeyManagement.Set((int)KeyManagementType.None);
                conf.WepTxKeyIndex = 0;
            }
            else if (type == 3)//WPA
            {
                conf.PreSharedKey = $"\"{Password}\"";
                conf.HiddenSSID = true;
                conf.AllowedAuthAlgorithms.Set((int)AuthAlgorithmType.Open);
                conf.AllowedGroupCiphers.Set((int)GroupCipherType.Tkip);
                conf.AllowedKeyManagement.Set((int)KeyManagementType.WpaPsk);
                conf.AllowedPairwiseCiphers.Set((int)PairwiseCipherType.Tkip);
                conf.AllowedGroupCiphers.Set((int)GroupCipherType.Ccmp);
                conf.AllowedPairwiseCiphers.Set((int)PairwiseCipherType.Ccmp);
                conf.StatusField = WifiStatus.Enabled;
            }
            return conf;
        }
        private class WifiReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent.Action == WifiManager.ScanResultsAvailableAction || intent.GetBooleanExtra(WifiManager.ExtraResultsUpdated, false))
                {
                    autoResetEvent.Set();
                }
            }
        }
        private class WifiReceiverStatus : BroadcastReceiver
        {
            public Action<Context, Intent> OnDataReceive = null;
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent.Action == WifiManager.NetworkStateChangedAction)
                {
                    OnDataReceive?.Invoke(context, intent);
                }
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                manager.Dispose();
                autoResetEvent.Dispose();
                ctx.UnregisterReceiver(wifiReceiver);
                ctx.UnregisterReceiver(wifiStatus);
                wifiReceiver.Dispose();
                wifiStatus.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}