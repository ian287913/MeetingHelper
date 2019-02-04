using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Controller;
using Controller.Component;
using System.Collections.ObjectModel;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace MeetingHelper
{
    public partial class App : Application
    {
        /// DJ
        public User user;
        public Room myRoom;
        //  Audio
        public AudioControl audioControl;

        //  Debug
        public ObservableCollection<DebugInfo> DebugList;
        public int Debug_Status;
        //  WiFi
        public WifiController mWifiController;
        //  Save & Load for Username
        const string Property_Username = "UserName";
        public string UserName { set; get; }
        //  Save & Load for Debug_LastSleep
        const string Property_LastSleep = "LastSleep";
        public bool LastSleep { set; get; }

        public App()
        {
            InitializeComponent();

            //  init User, WiFi and Audio
            //  done by SearchRoomPage

            //  init debug list
            DebugList = new ObservableCollection<DebugInfo>();
            Debug_Status = 0;

            //  Debug_LastSleep
            if (Properties.ContainsKey(Property_LastSleep))
                LastSleep = (bool)Properties[Property_LastSleep];
            Properties[Property_LastSleep] = false;

            //  UserName
            if (Properties.ContainsKey(Property_Username))
                UserName = (string)Properties[Property_Username];
            else
                UserName = "Unnamed";

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps

            // Debug_LastSleep
            Properties[Property_LastSleep] = true;
            // Save UserName
            Properties[Property_Username] = UserName;
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
    public class DebugInfo : BindableObject
    {
        public string Debug { get; set; }
        public string Debug_Count
        {
            get { return Count.ToString(); }
            set { }
        }
        public int Count { get; set; }
        public DebugInfo(string debug, int count)
        {
            Debug = debug;
            Count = count;
        }
    }
}
