using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using System.Collections.ObjectModel;
using Controller;
using Controller.Component;
using System.Net;

/// <summary>
/// TODO: 防轉向、頂條、implementation
/// # host join failed but room exist
/// # User.OnRoomListChanged() 很常被觸發
/// # 進入房間失敗: Error - room doesn't exist
/// 
/// </summary>

namespace MeetingHelper
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SearchRoomPage : ContentPage
	{
        //  Include WiFi, User, Room...
        App app = Application.Current as App;
        
        //  Rooms ItemSource
        ObservableCollection<ianRoom> Rooms;
        //  Layout control
        bool show_Warning;
        bool show_Password;
        bool show_Create;
        //  WiFi update
        bool Do_Update_WiFi;
        //  Target room (to join or create)
        ianRoom targetRoom;
        bool isTargetRoomFine = false;
        bool isHost = false;

        public SearchRoomPage ()
		{
			InitializeComponent ();
            ///  init DJ
            app.user.OnEnterRoom += User_OnEnterRoom;
            app.user.OnRoomListChanged += User_OnRoomListChanged;
            app.user.OnDuplicateName += User_OnDuplicateName;
            app.user.OnWrongPassword += User_OnWrongPassword;
            app.user.OnForbid += User_OnForbid;
            app.user.OnError += User_OnError;

            //  init Room ItemSource
            Rooms = new ObservableCollection<ianRoom>();
            ListView_Rooms.ItemsSource = Rooms;
            /// FAKE DATA
            //Rooms.Add(new ianRoom("Alpha Room", "Ian287913", "2019/1/21 19:35", ""));
            //Rooms.Add(new ianRoom("Bravo", "Founder", "2018/1/1 19:08", "ABCD"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            //Rooms.Add(new ianRoom("Charlie", "someone", "1998/12/16 07:32", "1234"));
            /// FAKE DATA

            //  init WiFi
            app.mWifiController.OnNetworkChanged += OnStatusChanged;
            //  Update WiFi
            Update_WiFi();
            
            //  click event
            ListView_Rooms.ItemTapped += (sender, e) =>
            {
                //  get target room
                targetRoom = e.Item as ianRoom;

                ((ListView)sender).SelectedItem = null;
                //  enable join layout
                Device.BeginInvokeOnMainThread(() =>
                {
                    Password_Title_Label.Text = targetRoom.Name;
                    Password_Entry.Text = "";
                    Password_Label.IsVisible = false;
                    Password_Error_Label.IsVisible = false;
                    Password_Error_Label.Text = "";

                    show_Password = true;
                    show_Layout();
                });
            };
        }
        
        //  Initialyze page
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            Device.BeginInvokeOnMainThread(() =>
            {
                //  updata room
                Rooms.Clear();
                /// use new DJ's properties
                foreach (RoomInfo room in app.user.RoomList)
                    Rooms.Add(new ianRoom(room.Name, room.Host, room.CreatedAt.ToLocalTime().ToShortDateString(), room.Locked, room.Address.ToString()));
                //  Update WiFi
                Do_Update_WiFi = true;
                //  init Entry
                Create_Password_Entry.Text = "";
                Create_RoomName_Entry.Text = "";
                Password_Entry.Text = "";
                Create_Label.Text = "";
                Create_Label.IsVisible = false;
                Create_Error_Label.Text = "";
                Create_Error_Label.IsVisible = false;
                //  init Visibility
                show_Warning = false;
                show_Password = false;
                show_Create = false;
                show_Layout();
            });
            //  Start Search room
            app.user.StartListener();
        }

        //  Async - Update WiFi info
        async void Update_WiFi()
        {
            await Task.Delay(1000);
            while(Do_Update_WiFi)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (app.mWifiController.currentStatus.State.Equals(WifiDetailedState.Connected))
                    {
                        //  get current IP
                        string strHostName = Dns.GetHostName();
                        IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
                        IPAddress ip = iphostentry.AddressList[0];
                        Label_WiFi_Name.Text = app.mWifiController.ConnectionInfo.SSID;
                        Label_WiFi_Content.Text = $"IP: {ip.ToString()}\nLink Speed: {app.mWifiController.ConnectionInfo.LinkSpeed} Mbps";
                    }
                    else
                    {
                        Label_WiFi_Name.Text = app.mWifiController.currentStatus.State.ToString();
                        Label_WiFi_Content.Text = "Please connect to any WiFi to join or create a room.";
                    }
                });
                await Task.Delay(500);
            }
        }
        
        //  update Layouts' visibility
        private void show_Layout()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Main_Layout.IsEnabled = false;

                Create_Layout.IsVisible = false;
                Create_Layout.IsEnabled = false;
                Warning_Layout.IsVisible = false;
                Warning_Layout.IsEnabled = false;
                Password_Layout.IsVisible = false;
                Password_Layout.IsEnabled = false;

                if (show_Warning)
                {
                    Warning_Layout.IsVisible = true;
                    Warning_Layout.IsEnabled = true;
                }
                else if (show_Create)
                {
                    Create_Layout.IsVisible = true;
                    Create_Layout.IsEnabled = true;
                }
                else if (show_Password)
                {
                    Password_Layout.IsVisible = true;
                    Password_Layout.IsEnabled = true;
                }
                else
                    Main_Layout.IsEnabled = true;
            });
        }

        //  disable BackButton
        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        //  Create a Room (host)
        async private void CreateRoom()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                isTargetRoomFine = true;
                isHost = true;
                //  notification
                Create_Label.TextColor = Color.FromHex("44FF44");
                Create_Label.Text = "establishing...";
                Create_Label.IsVisible = true;
                Create_Error_Label.Text = "";

                //  Create a room
                app.myRoom = new Room(targetRoom.Name, targetRoom.Password);
                app.myRoom.Open();
                app.myRoom.StartBroadcast(1, TimeUnit.Hour);
            });
            //  wait for 0.5sec
            await Task.Delay(1000);
            Device.BeginInvokeOnMainThread(() =>
            {
                //  validate room
                if (isTargetRoomFine)
                {
                    Create_Label.Text = "trying to enter...";
                    //  get into the room
                    app.user.BecomeHost(targetRoom.Name, targetRoom.Password, app.UserName, IPAddress.Parse(targetRoom.IpAddress));
                    //  wait for enter event...
                }
                else
                {
                    //  create room failed
                    Create_Error_Label.Text = "[create failed]";
                    Create_Error_Label.IsVisible = true;
                }
            });
        }

        //  Enter a Room (guest)
        async private void EnterRoom()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                isTargetRoomFine = true;
                isHost = false;
                //  notification
                Password_Label.Text = "verifying...";
                Password_Label.IsVisible = true;
                Password_Error_Label.IsVisible = false;

                //  check room
                app.user.JoinRoom(app.UserName, Password_Entry.Text, targetRoom.Name);
            });
            //  wait for 0.5sec
            await Task.Delay(1000);
            Device.BeginInvokeOnMainThread(() =>
            {
                //  validate room
                if (isTargetRoomFine)
                {
                    Password_Label.Text = "trying to enter...";
                    //  wait for enter event...
                }
                else
                {
                    ///  failed (unknown)
                    //Warning("Unknown", "join failed.");
                }
            });
        }

        //  Go to NextPage (HostPage or GuestPage)
        private void NextPage()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                //  Stop updating WiFi
                Do_Update_WiFi = false;
                //  Stop Search room
                app.user.StopListener();
                /// there should be some way to check...
                if (isHost)
                {
                    Navigation.PushModalAsync(new HostPage());
                }
                else
                {
                    Navigation.PushModalAsync(new GuestPage());
                }
            });
        }


        #region User_Events
        private void User_OnEnterRoom(object sender, EventArgs e)
        {
            Create_Label.Text = "entering...";
            Password_Label.Text = "entering...";
            // Go to Host page
            NextPage();
        }

        private void User_OnRoomListChanged(object sender, EventArgs e)
        {
            //  get room list into listview
            Device.BeginInvokeOnMainThread(() =>
            {
                //Rooms_Temp.Clear();
                foreach (RoomInfo room in app.user.RoomList)
                {
                    /// use new DJ's properties
                    Rooms.Add(new ianRoom(room.Name, room.Host, room.CreatedAt.ToLocalTime().ToShortDateString(), room.Locked, room.Address.ToString()));
                }
            });
        }

        private void User_OnDuplicateName(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                isTargetRoomFine = false;
                //  notification
                Create_Error_Label.Text += "Same room-name existed\n";
                Create_Error_Label.IsVisible = true;
            });
        }

        private void User_OnWrongPassword(object sender, WrongPasswordEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                isTargetRoomFine = false;
                //  notification
                Password_Error_Label.Text = $"wrong password ({e.RemainingTimes})";
                Password_Error_Label.IsVisible = true;
            });
        }

        private void User_OnForbid(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                isTargetRoomFine = false;
                //  notification
                Create_Error_Label.Text += "request forbidden\n";
                Create_Error_Label.IsVisible = true;
                Password_Error_Label.Text += "request forbidden\n";
                Password_Error_Label.IsVisible = true;
            });
        }

        private void User_OnError(object sender, ErrorEventArgs e)
        {
            Warning("Error_01", e.Exception.Message);
        }
        #endregion

        #region delegeates
        //  (B) Create Room
        private void OnClicked_Create(object sender, EventArgs e)
        {
            show_Create = true;
            show_Layout();
        }
        //  (B) Create Confirm (Validation)
        private void Create_Confirm_Clicked(object sender, EventArgs e)
        {
            if(Create_RoomName_Entry.Text.Length > 12)
            {
                Create_Label.TextColor = Color.FromHex("FF4444");
                Create_Label.Text = "the length of Room-Name should less than 12.";
                Create_Label.IsVisible = true;
            }
            else if (Create_RoomName_Entry.Text == "")
            {
                Create_Label.TextColor = Color.FromHex("FF4444");
                Create_Label.Text = "Room-Name can not be unnamed.";
                Create_Label.IsVisible = true;
            }
            else if(Create_Password_Entry.Text.Length > 8)
            {
                Create_Label.TextColor = Color.FromHex("FF4444");
                Create_Label.Text = "the length of Password should less than 8.";
                Create_Label.IsVisible = true;
            }
            else
            {
                //  get current IP
                string strHostName = Dns.GetHostName();
                IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
                IPAddress ip = iphostentry.AddressList[0];
                //  Create a target room
                targetRoom = new ianRoom(Create_RoomName_Entry.Text, app.UserName, DateTime.Now.ToLocalTime().ToShortTimeString(), Create_Password_Entry.Text, ip.ToString());
                CreateRoom();
            }
        }

        //  (B) Enter Password
        private void Password_Clicked(object sender, EventArgs e)
        {
            EnterRoom();
        }

        //  (B) Go back
        private void GoBack(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Password_Error_Label.IsVisible = false;
                Password_Entry.Text = "";
                Create_Label.IsVisible = false;
                Create_Error_Label.IsVisible = false;
                Create_RoomName_Entry.Text = "";
                Create_Password_Entry.Text = "";

                show_Create = false;
                show_Password = false;
                show_Layout();
            });
        }

        //  (W) WiFi status changed
        private void OnStatusChanged(object sender, NetworkChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (e.State.Equals(WifiDetailedState.Connected))
                {
                    //  get current IP
                    string strHostName = Dns.GetHostName();
                    IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
                    IPAddress ip = iphostentry.AddressList[0];
                    Label_WiFi_Name.Text = app.mWifiController.ConnectionInfo.SSID;
                    Label_WiFi_Content.Text = $"IP: {ip.ToString()}\nLink Speed: {app.mWifiController.ConnectionInfo.LinkSpeed} Mbps";
                }
                else
                {
                    Label_WiFi_Name.Text = e.State.ToString();
                    Label_WiFi_Content.Text = "Please connect to any WiFi to join or create a room.";
                }
            });
        }
        
        //  Warning
        private void Warning(string title, string message)
        {
            //  show warning and disable main layout
            Device.BeginInvokeOnMainThread(() =>
            {
                Warning_Title.Text = title;
                Warning_Content.Text = message;
                show_Warning = true;
                show_Layout();
            });
        }
        //  (B) Warning
        private void Warning_Clicked(object sender, EventArgs e)
        {
            show_Warning = false;
            show_Layout();
        }

        #endregion
    }

    public class ianRoom : BindableObject
    {
        public string Name { get; set; }
        public string Founder { get; set; }
        public string Found_Time { get; set; }
        public string IpAddress { get; set; }
        public bool IsLocked { get; set; }
        public string Password { get; set; }
        //private bool _isSelected;
        //public bool IsSelected
        //{
        //    get { return _isSelected; }
        //    set
        //    {
        //        _isSelected = value;
        //        OnPropertyChanged("IsSelected");
        //    }
        //}

        public ianRoom(string name, string founder, string foundTime, bool isLocked, string ipAddress)
        {
            Name = name;
            Founder = founder;
            Found_Time = foundTime;
            IsLocked = isLocked;
            IpAddress = ipAddress;
            //_isSelected = false;
            Password = "unknown";
        }
        public ianRoom(string name, string founder, string foundTime, string password, string ipAddress)
        {
            Name = name;
            Founder = founder;
            Found_Time = foundTime;
            Password = password;
            IpAddress = ipAddress;
            if (password == "")
                IsLocked = false;
            else
                IsLocked = true;
        }
    }
}